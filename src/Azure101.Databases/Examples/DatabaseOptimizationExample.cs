using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Azure101.Databases.Examples
{
    /// <summary>
    /// Database Query Optimization Patterns
    /// Demonstrates: Index usage, avoiding N+1 queries, query tuning
    /// Typical optimization: 30-second query → 100-millisecond query (300x improvement)
    /// </summary>
    public class DatabaseOptimizationExample
    {
        private readonly string _connectionString;

        public DatabaseOptimizationExample(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ============================================================================
        // Problem 1: Missing Index on WHERE Clause
        // ============================================================================
        /// <summary>
        /// BEFORE: SELECT * FROM Orders WHERE CustomerId = 5
        /// Execution: TABLE SCAN (reads all 1M rows!)
        /// Time: 30 seconds
        ///
        /// AFTER: CREATE INDEX ix_orders_customer ON Orders(CustomerId)
        /// Execution: INDEX SEEK (reads only matching rows)
        /// Time: 100 milliseconds
        /// IMPROVEMENT: 300x faster
        /// </summary>
        public async Task<List<Order>> GetCustomerOrdersOptimizedAsync(int customerId)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Query that benefits from index on CustomerId
                    const string query = @"
                        SELECT OrderId, CustomerId, TotalAmount, CreatedDate
                        FROM Orders
                        WHERE CustomerId = @customerId
                        ORDER BY CreatedDate DESC";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@customerId", customerId);

                        var orders = new List<Order>();

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                orders.Add(new Order
                                {
                                    OrderId = (int)reader["OrderId"],
                                    CustomerId = (int)reader["CustomerId"],
                                    TotalAmount = (decimal)reader["TotalAmount"],
                                    CreatedDate = (DateTime)reader["CreatedDate"]
                                });
                            }
                        }

                        sw.Stop();
                        Console.WriteLine($"✓ Retrieved {orders.Count} orders in {sw.ElapsedMilliseconds}ms");
                        return orders;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Problem 2: N+1 Query Anti-Pattern
        // ============================================================================
        /// <summary>
        /// ANTI-PATTERN (DON'T DO THIS):
        /// 1. Query: SELECT * FROM Customers (1000 rows)
        /// 2. Loop: For each customer, query orders (1000 queries!)
        /// Total: 1001 queries, 30+ seconds
        ///
        /// SOLUTION:
        /// 1. Query: SELECT c.*, o.* FROM Customers c JOIN Orders o ON c.Id = o.CustomerId
        /// 2. Load in memory with group by
        /// Total: 1 query, 100 milliseconds
        /// </summary>
        public async Task<List<CustomerWithOrders>> GetCustomersWithOrdersOptimizedAsync()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Single query with JOIN (not N+1)
                    const string query = @"
                        SELECT
                            c.CustomerId,
                            c.Name,
                            c.Email,
                            o.OrderId,
                            o.TotalAmount,
                            o.CreatedDate
                        FROM Customers c
                        LEFT JOIN Orders o ON c.CustomerId = o.CustomerId
                        ORDER BY c.CustomerId, o.CreatedDate DESC";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        command.CommandTimeout = 30;

                        var customersDict = new Dictionary<int, CustomerWithOrders>();

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int customerId = (int)reader["CustomerId"];

                                // Get or create customer
                                if (!customersDict.ContainsKey(customerId))
                                {
                                    customersDict[customerId] = new CustomerWithOrders
                                    {
                                        CustomerId = customerId,
                                        Name = reader["Name"].ToString(),
                                        Email = reader["Email"].ToString(),
                                        Orders = new List<Order>()
                                    };
                                }

                                // Add order if not null
                                if (reader["OrderId"] != DBNull.Value)
                                {
                                    customersDict[customerId].Orders.Add(new Order
                                    {
                                        OrderId = (int)reader["OrderId"],
                                        TotalAmount = (decimal)reader["TotalAmount"],
                                        CreatedDate = (DateTime)reader["CreatedDate"]
                                    });
                                }
                            }
                        }

                        sw.Stop();
                        Console.WriteLine($"✓ Retrieved {customersDict.Count} customers with orders in {sw.ElapsedMilliseconds}ms");
                        return customersDict.Values.ToList();
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Problem 3: Function on Column Prevents Index Usage
        // ============================================================================
        /// <summary>
        /// BAD QUERY:
        /// SELECT * FROM Orders WHERE YEAR(CreatedDate) = 2023
        /// Reason: Function YEAR() is applied to column, so index can't be used
        /// Result: TABLE SCAN of all rows, 30 seconds
        ///
        /// GOOD QUERY:
        /// SELECT * FROM Orders
        /// WHERE CreatedDate >= '2023-01-01' AND CreatedDate < '2024-01-01'
        /// Reason: Range comparison allows index usage
        /// Result: INDEX SEEK, 100 milliseconds
        /// </summary>
        public async Task<List<Order>> GetOrdersByYearOptimizedAsync(int year)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Optimized: Range comparison instead of function
                    string query = @"
                        SELECT OrderId, CustomerId, TotalAmount, CreatedDate
                        FROM Orders
                        WHERE CreatedDate >= @startDate AND CreatedDate < @endDate
                        ORDER BY CreatedDate DESC";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        var startDate = new DateTime(year, 1, 1);
                        var endDate = new DateTime(year + 1, 1, 1);

                        command.Parameters.AddWithValue("@startDate", startDate);
                        command.Parameters.AddWithValue("@endDate", endDate);

                        var orders = new List<Order>();

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                orders.Add(new Order
                                {
                                    OrderId = (int)reader["OrderId"],
                                    CustomerId = (int)reader["CustomerId"],
                                    TotalAmount = (decimal)reader["TotalAmount"],
                                    CreatedDate = (DateTime)reader["CreatedDate"]
                                });
                            }
                        }

                        sw.Stop();
                        Console.WriteLine($"✓ Retrieved {orders.Count} orders from {year} in {sw.ElapsedMilliseconds}ms");
                        return orders;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Problem 4: SELECT * Fetches Unnecessary Columns
        // ============================================================================
        /// <summary>
        /// BAD:
        /// SELECT * FROM Orders -- Fetches all 20 columns including large text fields
        /// Network: 10MB transferred for 1000 rows
        /// Time: 5 seconds
        ///
        /// GOOD:
        /// SELECT OrderId, CustomerId, TotalAmount FROM Orders -- Only needed columns
        /// Network: 0.5MB transferred
        /// Time: 100 milliseconds
        /// IMPROVEMENT: 50x faster
        /// </summary>
        public async Task<List<OrderSummary>> GetOrderSummariesOptimizedAsync()
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Optimized: Only fetch needed columns
                    const string query = @"
                        SELECT
                            OrderId,
                            CustomerId,
                            TotalAmount,
                            CreatedDate
                        FROM Orders
                        ORDER BY CreatedDate DESC";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        var summaries = new List<OrderSummary>();

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                summaries.Add(new OrderSummary
                                {
                                    OrderId = (int)reader["OrderId"],
                                    CustomerId = (int)reader["CustomerId"],
                                    TotalAmount = (decimal)reader["TotalAmount"]
                                });
                            }
                        }

                        sw.Stop();
                        Console.WriteLine($"✓ Retrieved {summaries.Count} order summaries in {sw.ElapsedMilliseconds}ms");
                        return summaries;
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Problem 5: Query Optimization Recommendations
        // ============================================================================
        /// <summary>
        /// Use SQL Server Query Store to find slow queries
        /// Steps:
        /// 1. Enable Query Store in database
        /// 2. Run: SELECT TOP 10 * FROM sys.query_store_query_text
        /// 3. Find queries with longest duration
        /// 4. Analyze execution plan (look for table scans)
        /// 5. Add missing indexes
        /// 6. Rerun query to verify improvement
        /// </summary>
        public async Task AnalyzeSlowQueriesAsync()
        {
            try
            {
                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get top 10 slowest queries from Query Store
                    const string query = @"
                        SELECT TOP 10
                            qt.text,
                            q.execution_count,
                            rs.avg_duration / 1000000.0 as avg_duration_ms,
                            rs.max_duration / 1000000.0 as max_duration_ms
                        FROM sys.query_store_query_text qt
                        INNER JOIN sys.query_store_query q ON qt.query_text_id = q.query_text_id
                        INNER JOIN sys.query_store_runtime_stats rs ON q.query_id = rs.query_id
                        ORDER BY avg_duration_ms DESC";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        Console.WriteLine("\n=== Top 10 Slowest Queries ===");

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            int rank = 1;
                            while (await reader.ReadAsync())
                            {
                                Console.WriteLine($"\n{rank}. {reader["text"].ToString().Substring(0, 80)}...");
                                Console.WriteLine($"   Executions: {reader["execution_count"]}");
                                Console.WriteLine($"   Avg Duration: {reader["avg_duration_ms"]}ms");
                                Console.WriteLine($"   Max Duration: {reader["max_duration_ms"]}ms");
                                rank++;
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Query Store error: {ex.Message}");
            }
        }

        // ============================================================================
        // Covering Index Example
        // ============================================================================
        /// <summary>
        /// Regular index: Only includes columns in WHERE clause
        /// Problem: Still needs table lookup for other columns
        ///
        /// Covering index: Includes all columns needed by query
        /// Benefit: No table lookup needed (index has all data)
        /// Performance: 5x faster than regular index
        /// </summary>
        public static class IndexOptimization
        {
            // Regular index (not covering)
            public const string REGULAR_INDEX = @"
                CREATE INDEX ix_orders_customer ON Orders(CustomerId)";

            // Covering index (includes columns from SELECT)
            public const string COVERING_INDEX = @"
                CREATE INDEX ix_orders_customer_covering
                ON Orders(CustomerId)
                INCLUDE (TotalAmount, CreatedDate)";

            // Query that benefits from covering index
            public const string OPTIMIZED_QUERY = @"
                SELECT OrderId, CustomerId, TotalAmount, CreatedDate
                FROM Orders
                WHERE CustomerId = @customerId";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class Order
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OrderSummary
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CustomerWithOrders
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public List<Order> Orders { get; set; }
    }
}
