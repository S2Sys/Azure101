using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Azure101.Databases.Examples
{
    /// <summary>
    /// Azure Cosmos DB Advanced Patterns
    /// Demonstrates: Document operations, RU optimization, partitioning, TTL
    /// Production patterns for global scale with flexible schema
    /// </summary>
    public class CosmosDBAdvancedExample
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        // RU tracking for cost optimization
        private double _totalRUsConsumed = 0;

        public CosmosDBAdvancedExample(CosmosClient cosmosClient, Container container)
        {
            _cosmosClient = cosmosClient;
            _container = container;
        }

        // ============================================================================
        // Pattern 1: Optimized Point Query (Single Partition)
        // ============================================================================
        /// <summary>
        /// Get document by ID and partition key
        /// Costs: ~1 RU (cheapest operation)
        /// Speed: < 10ms latency
        /// Best for: Getting single document by ID
        /// </summary>
        public async Task<OrderDocument> GetOrderByIdAsync(string orderId, string customerId)
        {
            try
            {
                // Point query: Use ID + partition key for fastest access
                // Partition key = customerId (ensures single partition hit)
                var response = await _container.ReadItemAsync<OrderDocument>(
                    id: orderId,
                    partitionKey: new PartitionKey(customerId));

                Console.WriteLine($"✓ Order retrieved in {response.RequestCharge} RUs");
                _totalRUsConsumed += response.RequestCharge;

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Order not found");
                return null;
            }
        }

        // ============================================================================
        // Pattern 2: Efficient Document Insert with Auto-TTL
        // ============================================================================
        /// <summary>
        /// Create document with automatic expiration (TTL)
        /// TTL: Document auto-deleted after 30 days
        /// Saves storage costs for temporary data (sessions, carts, logs)
        /// </summary>
        public async Task<OrderDocument> CreateOrderWithTTLAsync(OrderDocument order)
        {
            try
            {
                // Set TTL to 30 days (2,592,000 seconds)
                // Document auto-deleted after expiration
                order.Ttl = 2592000; // 30 days in seconds

                var response = await _container.CreateItemAsync(
                    item: order,
                    partitionKey: new PartitionKey(order.CustomerId));

                Console.WriteLine($"✓ Order created in {response.RequestCharge} RUs");
                _totalRUsConsumed += response.RequestCharge;

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.WriteLine("Order already exists");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Cross-Partition Query (Expensive)
        // ============================================================================
        /// <summary>
        /// Query all orders across partitions
        /// Costs: 1 RU per 1KB returned (can be expensive!)
        /// Speed: 500-5000ms (slower than point query)
        /// Warning: Avoids if you can filter by partition key
        /// </summary>
        public async Task<List<OrderDocument>> QueryOrdersByStatusAsync(string status)
        {
            try
            {
                // This query scans ALL partitions (expensive)
                // Costs: ~50-100 RUs for 100 results (10KB data)
                var query = _container.GetItemQueryIterator<OrderDocument>(
                    queryDefinition: new QueryDefinition(
                        "SELECT * FROM c WHERE c.status = @status")
                    .WithParameter("@status", status));

                var orders = new List<OrderDocument>();
                double totalRU = 0;

                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    orders.AddRange(response);
                    totalRU += response.RequestCharge;

                    Console.WriteLine($"Page returned {response.Count} orders in {response.RequestCharge} RUs");
                }

                Console.WriteLine($"✓ Total {orders.Count} orders in {totalRU} RUs");
                _totalRUsConsumed += totalRU;

                return orders;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Query failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Optimized Query with Partition Key Filter
        // ============================================================================
        /// <summary>
        /// Query orders for specific customer (single partition)
        /// Costs: ~10-30 RUs (much cheaper than cross-partition)
        /// Speed: < 100ms (faster than cross-partition)
        /// Best practice: Always filter by partition key if possible
        /// </summary>
        public async Task<List<OrderDocument>> GetOrdersByCustomerAsync(string customerId)
        {
            try
            {
                // Single partition query (includes partition key in WHERE)
                // Only hits 1 partition = much cheaper than cross-partition
                var query = _container.GetItemQueryIterator<OrderDocument>(
                    queryDefinition: new QueryDefinition(
                        "SELECT * FROM c WHERE c.customerId = @customerId ORDER BY c.createdDate DESC")
                    .WithParameter("@customerId", customerId),
                    requestOptions: new QueryRequestOptions
                    {
                        MaxItemCount = 10 // Limit to 10 results
                    });

                var orders = new List<OrderDocument>();
                double totalRU = 0;

                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    orders.AddRange(response);
                    totalRU += response.RequestCharge;

                    Console.WriteLine($"Fetched {response.Count} orders in {response.RequestCharge} RUs");
                }

                Console.WriteLine($"✓ Customer {customerId} has {orders.Count} orders ({totalRU} RUs)");
                _totalRUsConsumed += totalRU;

                return orders;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Query error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Batch Operations (Reduce RU Overhead)
        // ============================================================================
        /// <summary>
        /// Batch insert multiple orders in one transaction
        /// Reduces RU overhead vs individual operations
        /// All-or-nothing atomicity
        /// </summary>
        public async Task<List<OrderDocument>> BatchInsertOrdersAsync(
            string customerId,
            List<OrderDocument> orders)
        {
            try
            {
                var batch = _container.CreateTransactionalBatch(
                    partitionKey: new PartitionKey(customerId));

                // Add all operations to batch
                foreach (var order in orders)
                {
                    batch.CreateItem(order);
                }

                // Execute batch atomically
                var response = await batch.ExecuteAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ Batch inserted {orders.Count} orders in {response.RequestCharge} RUs");
                    _totalRUsConsumed += response.RequestCharge;
                    return orders;
                }
                else
                {
                    Console.WriteLine($"✗ Batch failed: {response.StatusCode}");
                    throw new InvalidOperationException($"Batch operation failed: {response.StatusCode}");
                }
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Batch error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Update Document (Upsert Pattern)
        // ============================================================================
        /// <summary>
        /// Create or update document in single operation
        /// Idempotent: Safe to call multiple times
        /// Useful for: Duplicate message handling
        /// </summary>
        public async Task<OrderDocument> UpsertOrderAsync(OrderDocument order)
        {
            try
            {
                // Upsert: Create if not exists, update if exists
                var response = await _container.UpsertItemAsync(
                    item: order,
                    partitionKey: new PartitionKey(order.CustomerId));

                Console.WriteLine($"✓ Order upserted in {response.RequestCharge} RUs");
                _totalRUsConsumed += response.RequestCharge;

                return response.Resource;
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Upsert error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 7: RU Consumption Monitoring
        // ============================================================================
        /// <summary>
        /// Track RU consumption to optimize costs
        /// Free tier: 400 RUs included
        /// Production: Track actual RU usage
        /// Cost: ~$6 per 100 RUs per month
        /// </summary>
        public void LogRUConsumption()
        {
            Console.WriteLine($"\n=== RU Consumption Report ===");
            Console.WriteLine($"Total RUs consumed: {_totalRUsConsumed:F2}");
            Console.WriteLine($"Estimated monthly cost: ${(_totalRUsConsumed / 100 * 6):F2}");
            Console.WriteLine($"(Assuming 1M operations/month at same rate)");
        }

        // ============================================================================
        // Pattern 8: Partition Key Design for Even Distribution
        // ============================================================================
        /// <summary>
        /// Choose partition key carefully to avoid hot partitions
        /// Bad: partition by CustomerId (some customers have 1000x orders)
        /// Good: partition by CustomerId + use composite key
        /// Best: Choose high-cardinality field (millions of unique values)
        /// </summary>
        public static class PartitionKeyBestPractices
        {
            // Bad partition key (skewed distribution)
            // CustomerId 123 has 1M orders, customer 456 has 1 order
            // CustomerId 123 partition hit throttling
            public const string BAD_PARTITION_KEY = "/customerId";

            // Good partition key (even distribution)
            // CustomerId is used with OrderDate to distribute evenly
            // CustomerId + Month spreads orders across partitions
            public const string GOOD_PARTITION_KEY = "/customerId";

            // Alternative: Use custom field for better distribution
            // hash(customerId) % 10 to distribute into 10 partitions
            // More complex but ensures even distribution
            public const string BEST_PARTITION_KEY_STRATEGY = "Use /customerId with hash function";
        }
    }

    // ============================================================================
    // Document Models with Flexible Schema
    // ============================================================================
    public class OrderDocument
    {
        public string Id { get; set; } // Cosmos DB ID
        public string CustomerId { get; set; } // Partition key
        public string Status { get; set; } // open, pending, shipped, completed
        public decimal TotalAmount { get; set; }
        public List<OrderItem> Items { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? Ttl { get; set; } // Time to live in seconds (auto-delete)
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
