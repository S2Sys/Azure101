using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azure101.DataAnalytics.Examples
{
    /// <summary>
    /// Azure Synapse Analytics - Data Warehouse + Big Data Analytics
    /// Demonstrates: SQL queries, Spark notebooks, data integration, MPP
    /// Production pattern: Query petabytes of data efficiently
    /// Benefits: 10x faster than traditional data warehouses
    /// </summary>
    public class SynapseAnalyticsExample
    {
        private readonly string _connectionString;
        private readonly ILogger<SynapseAnalyticsExample> _logger;

        public SynapseAnalyticsExample(
            string synapseWorkspaceName,
            string synapseDatabase,
            ILogger<SynapseAnalyticsExample> logger)
        {
            _logger = logger;
            _connectionString = $"Server=tcp:{synapseWorkspaceName}.sql.azuresynapse.net,1433;" +
                               $"Initial Catalog={synapseDatabase};" +
                               $"Persist Security Info=False;User ID=sqladmin;" +
                               $"Password=YourPassword;MultipleActiveResultSets=False;" +
                               $"Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        }

        // ============================================================================
        // Pattern 1: MPP (Massively Parallel Processing)
        // ============================================================================
        /// <summary>
        /// Synapse distributes query across 60+ nodes
        /// Benefits: 1PB query in seconds (vs hours in SQL Database)
        /// Architecture: Control node + 60 compute nodes
        /// </summary>
        public class MPPArchitecture
        {
            public const string CONCEPT = @"
                TRADITIONAL SQL DATABASE (Single Server):
                Query: SELECT * FROM 1PB table WHERE date = '2024-01-15'
                Process: Single server scans entire 1PB
                Time: 2+ hours
                Bottleneck: Single CPU

                SYNAPSE (60 Node Cluster):
                Query: Same query
                Process:
                  - Control node: Receive query
                  - Split into 60 tasks (one per node)
                  - Each node scans its 16TB partition in parallel
                  - Aggregate results
                Time: 2 seconds
                Speed: 3600x faster!

                Why?
                ✓ Parallelism: 60 nodes work simultaneously
                ✓ Distributed: Data already partitioned
                ✓ Optimized: Columnar storage (Parquet-like)
                ✓ Scalable: Add more nodes = more speed";

            public const string USE_CASE = @"
                Scale: 1TB → 1PB
                Time: 100ms → 100s (no change!)
                Because: 1TB on 60 nodes = 1PB on 60 nodes
                More data, same speed (distributed)";
        }

        // ============================================================================
        // Pattern 2: Execute Data Warehouse Query
        // ============================================================================
        /// <summary>
        /// Query huge datasets efficiently
        /// </summary>
        public async Task<List<SalesAnalytics>> QuerySalesAnalyticsAsync(
            int year,
            string region)
        {
            try
            {
                _logger.LogInformation($"→ Querying sales for {year} in {region}");

                var results = new List<SalesAnalytics>();

                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    const string query = @"
                        SELECT
                            DATEPART(YEAR, SaleDate) AS Year,
                            DATEPART(MONTH, SaleDate) AS Month,
                            Region,
                            SUM(Amount) AS TotalSales,
                            COUNT(*) AS TransactionCount,
                            AVG(Amount) AS AvgSale
                        FROM dbo.Sales
                        WHERE DATEPART(YEAR, SaleDate) = @Year
                            AND Region = @Region
                        GROUP BY
                            DATEPART(YEAR, SaleDate),
                            DATEPART(MONTH, SaleDate),
                            Region
                        ORDER BY Month";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);
                        command.Parameters.AddWithValue("@Region", region);
                        command.CommandTimeout = 300; // 5 minutes

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new SalesAnalytics
                                {
                                    Year = (int)reader["Year"],
                                    Month = (int)reader["Month"],
                                    Region = reader["Region"].ToString(),
                                    TotalSales = (decimal)reader["TotalSales"],
                                    TransactionCount = (int)reader["TransactionCount"],
                                    AvgSale = (decimal)reader["AvgSale"]
                                });
                            }
                        }
                    }
                }

                _logger.LogInformation($"✓ Retrieved {results.Count} months of data");
                foreach (var row in results)
                {
                    _logger.LogInformation(
                        $"  {row.Year}-{row.Month:D2}: ${row.TotalSales:N0} ({row.TransactionCount} transactions)");
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Query failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Data Integration (ETL)
        // ============================================================================
        /// <summary>
        /// Extract data from multiple sources, transform, load to Synapse
        /// Process: Raw → Cleaned → Denormalized → Ready for analysis
        /// </summary>
        public async Task<bool> RunETLPipelineAsync()
        {
            try
            {
                _logger.LogInformation("→ Running ETL pipeline");

                // Step 1: Extract from ADLS
                _logger.LogInformation("  [1/5] Extracting from Data Lake...");
                var extracted = await ExtractDataAsync();

                // Step 2: Validate
                _logger.LogInformation("  [2/5] Validating data quality...");
                var validated = await ValidateDataAsync(extracted);

                // Step 3: Transform
                _logger.LogInformation("  [3/5] Transforming data...");
                var transformed = await TransformDataAsync(validated);

                // Step 4: Aggregate
                _logger.LogInformation("  [4/5] Aggregating dimensions...");
                var aggregated = await AggregateDataAsync(transformed);

                // Step 5: Load to Synapse
                _logger.LogInformation("  [5/5] Loading to Synapse...");
                await LoadDataAsync(aggregated);

                _logger.LogInformation("✓ ETL pipeline complete");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ ETL failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Dedicated vs Serverless Pools
        // ============================================================================
        public static class PoolTypes
        {
            public const string DEDICATED_POOL = @"
                Dedicated SQL Pool (Traditional data warehouse):
                - Fixed compute capacity (DW100c → DW30000c)
                - Always running (costs $$$)
                - Best: Predictable workloads, 24/7 queries
                - Pricing: $2.5/hour per DW unit

                DW100c tier:
                - 100 compute units
                - Cost: $250/month (always on)
                - Speed: ~10GB/second scan

                DW1000c tier:
                - 1000 compute units
                - Cost: $2,500/month
                - Speed: ~100GB/second scan (10x faster)

                When to use:
                ✓ Heavy analytics load (1000+ queries/day)
                ✓ Real-time dashboards
                ✓ Predictable resource needs";

            public const string SERVERLESS_POOL = @"
                Serverless SQL Pool (Pay-per-query):
                - Auto-scales (no provisioning)
                - Pay only for what you scan
                - Best: Ad-hoc queries, variable workload
                - Pricing: $5.90 per TB scanned

                Example costs:
                Query scans 10GB: 0.01TB × $5.90 = $0.059
                1000 queries/day × 10GB = 10TB/day
                Cost: 10TB × $5.90 = $59/day = $1,770/month

                When to use:
                ✓ Occasional queries
                ✓ Unpredictable workload
                ✓ Ad-hoc analysis
                ✓ Don't want uptime costs";

            public const string DECISION = @"
                Use Dedicated if:
                - Daily/hourly queries
                - Business critical (24/7)
                - High volume (100+ queries/hour)
                - Predictable needs

                Use Serverless if:
                - Occasional queries
                - Development/testing
                - Variable workload
                - Want to avoid idle costs

                Hybrid:
                Use both! Serverless for ad-hoc, Dedicated for BI";
        }

        // ============================================================================
        // Pattern 5: Spark Notebooks (Big Data Processing)
        // ============================================================================
        /// <summary>
        /// Use Spark (distributed processing framework) for complex transformations
        /// </summary>
        public static class SparkNotebooks
        {
            public const string EXAMPLE_WORKFLOW = @"
                Synapse Spark Notebook:

                # Step 1: Load data
                df = spark.read.parquet('/data/raw/sales/*.parquet')

                # Step 2: Transform
                df = df.filter(col('amount') > 100)
                  .groupBy('customer_id')
                  .agg(sum('amount').alias('total_spend'))

                # Step 3: Enrich (join with dimension)
                customers = spark.read.parquet('/data/customers.parquet')
                result = df.join(customers, 'customer_id')

                # Step 4: Save result
                result.write.mode('overwrite')\
                  .parquet('/data/processed/high_value_customers/')

                Why use Spark in Synapse?
                ✓ Complex transformations (harder in SQL)
                ✓ Distributed processing (fast on big data)
                ✓ Integration (write to Synapse tables)
                ✓ Flexible (Python, Scala, Spark SQL)";

            public const string USE_CASES = @"
                Use Spark for:
                ✓ Complex transformations
                ✓ Machine learning preprocessing
                ✓ Unstructured data (logs, text)
                ✓ Graph processing
                ✓ Custom algorithms

                Use SQL for:
                ✓ Simple aggregations
                ✓ Business reports
                ✓ Ad-hoc analysis
                ✓ When performance critical";
        }

        // ============================================================================
        // Pattern 6: Performance Best Practices
        // ============================================================================
        public static class PerformanceTips
        {
            public const string OPTIMIZATION = @"
                1. DISTRIBUTION
                   Choose partition key carefully
                   Round-robin: For joins
                   Hash: For aggregation on that column
                   Replicate: For small dimensions

                2. INDEXING
                   Synapse uses clustered columnstore index
                   Batch inserts > 102,400 rows for compression

                3. AVOID FULL TABLE SCANS
                   ✗ WHERE name LIKE '%john%'  (scans all)
                   ✓ WHERE id = 123  (seeks to partition)

                4. USE CTAS (Create Table As Select)
                   CREATE TABLE new_table AS
                   SELECT * FROM old_table WHERE...
                   Faster: Parallel write + compression

                5. PARTITION ELIMINATION
                   ✓ WHERE date >= '2024-01-01' AND date < '2024-02-01'
                   → Reads only January partition
                   ✗ WHERE YEAR(date) = 2024
                   → Scans all years (can't eliminate)

                6. STATISTICS
                   Create statistics for columns used in WHERE
                   Statistics guide optimizer for better plans";

            public const string QUERY_EXAMPLE = @"
                SLOW QUERY:
                SELECT customer_id, SUM(amount)
                FROM sales
                WHERE YEAR(sale_date) = 2024
                GROUP BY customer_id
                Issues:
                - Function on column (can't use partition)
                - Full table scan (100GB)
                - Time: 30 seconds

                FAST QUERY:
                SELECT customer_id, SUM(amount)
                FROM sales
                WHERE sale_date >= '2024-01-01'
                  AND sale_date < '2025-01-01'
                GROUP BY customer_id
                Benefits:
                - Date range (partition eliminated)
                - Scans only 2024 data (8GB)
                - Time: 2 seconds
                Speed: 15x faster!";
        }

        // ============================================================================
        // Pattern 7: Synapse vs SQL Database vs Snowflake
        // ============================================================================
        public static class DWComparison
        {
            public const string COMPARISON = @"
                AZURE SQL DATABASE:
                Size: Up to 4TB
                Use: OLTP (operational), < 1TB
                Pricing: $150-5000/month
                Speed: Single server
                Concurrency: 100s of connections

                AZURE SYNAPSE (MPP Data Warehouse):
                Size: Unlimited petabytes
                Use: Analytics, 1TB-1PB
                Pricing: Flexible (dedicated or serverless)
                Speed: 60 nodes parallel
                Concurrency: 1000s of queries

                SNOWFLAKE (Cloud Data Platform):
                Size: Unlimited petabytes
                Use: Analytics, data sharing
                Pricing: Per credit used ($2-4 per credit)
                Speed: Cloud native, auto-scaling
                Concurrency: High (shared compute)

                WHEN TO USE:
                SQL Database: Transactional systems, app data
                Synapse: Enterprise analytics, big data
                Snowflake: Data sharing, multi-cloud";
        }

        // ============================================================================
        // Helper Methods
        // ============================================================================
        private async Task<List<object>> ExtractDataAsync()
        {
            await Task.Delay(100);
            return new List<object> { /* data */ };
        }

        private async Task<List<object>> ValidateDataAsync(List<object> data)
        {
            await Task.Delay(50);
            return data;
        }

        private async Task<List<object>> TransformDataAsync(List<object> data)
        {
            await Task.Delay(150);
            return data;
        }

        private async Task<List<object>> AggregateDataAsync(List<object> data)
        {
            await Task.Delay(100);
            return data;
        }

        private async Task<bool> LoadDataAsync(List<object> data)
        {
            await Task.Delay(200);
            _logger.LogInformation($"  Loaded {data.Count} records");
            return true;
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class SalesAnalytics
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Region { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AvgSale { get; set; }
    }
}
