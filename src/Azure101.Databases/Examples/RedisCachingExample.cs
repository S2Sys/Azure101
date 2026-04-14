using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Azure101.Databases.Examples
{
    /// <summary>
    /// Azure Cache for Redis - Production Patterns
    /// Demonstrates: Caching, cache-aside pattern, TTL, Pub/Sub, distributed locking
    /// Redis: In-memory key-value store for sub-millisecond performance
    /// </summary>
    public class RedisCachingExample
    {
        private readonly IDatabase _redis;
        private readonly ISubscriber _subscriber;

        public RedisCachingExample(IConnectionMultiplexer redisConnection)
        {
            _redis = redisConnection.GetDatabase();
            _subscriber = redisConnection.GetSubscriber();
        }

        // ============================================================================
        // Pattern 1: Cache-Aside Pattern (Most Common)
        // ============================================================================
        /// <summary>
        /// Check cache first, fall back to database if miss
        /// Hit: 1ms (ultra-fast)
        /// Miss: 50ms (database query)
        /// Typical hit rate: 90-95%
        /// Average latency: 5ms (vs 50ms without cache)
        /// </summary>
        public async Task<Product> GetProductAsync(int productId)
        {
            const string cacheKey = $"product:{productId}";

            try
            {
                // Step 1: Check cache
                var cachedValue = await _redis.StringGetAsync(cacheKey);

                if (cachedValue.HasValue)
                {
                    // Cache HIT: Return immediately
                    Console.WriteLine($"✓ Cache HIT for product:{productId}");
                    return JsonSerializer.Deserialize<Product>(cachedValue.ToString());
                }

                // Step 2: Cache MISS - Query database
                Console.WriteLine($"✗ Cache MISS for product:{productId} - querying database");
                var product = await FetchProductFromDatabaseAsync(productId);

                if (product != null)
                {
                    // Step 3: Store in cache (1 hour TTL)
                    var serialized = JsonSerializer.Serialize(product);
                    await _redis.StringSetAsync(
                        key: cacheKey,
                        value: serialized,
                        expiry: TimeSpan.FromHours(1));

                    Console.WriteLine($"✓ Product cached for 1 hour");
                }

                return product;
            }
            catch (RedisConnectionException ex)
            {
                // Redis down? Fall back to database
                Console.WriteLine($"⚠ Redis unavailable: {ex.Message} - using database directly");
                return await FetchProductFromDatabaseAsync(productId);
            }
        }

        // ============================================================================
        // Pattern 2: Cache Invalidation on Write
        // ============================================================================
        /// <summary>
        /// Update product and invalidate cache
        /// Cache must be cleared so next request fetches fresh data
        /// Two approaches: Delete (immediate) or TTL (lazy)
        /// </summary>
        public async Task<bool> UpdateProductAsync(int productId, Product product)
        {
            try
            {
                // Step 1: Update database
                var success = await UpdateProductInDatabaseAsync(productId, product);

                if (success)
                {
                    // Step 2: Invalidate cache (delete so next query fetches fresh)
                    const string cacheKey = $"product:{productId}";
                    await _redis.KeyDeleteAsync(cacheKey);
                    Console.WriteLine($"✓ Product updated and cache invalidated");
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Batch Invalidation (Category Change)
        // ============================================================================
        /// <summary>
        /// Invalidate all products in category
        /// When category price changes, invalidate all product caches
        /// Uses pattern-based deletion
        /// </summary>
        public async Task InvalidateCategoryAsync(string categoryId)
        {
            try
            {
                // Find all keys matching pattern: product:*
                // This is expensive in production (scan all keys)
                // Better approach: Maintain explicit list in Redis

                var server = GetRedisServer();

                // Scan keys with pattern (pattern scan, not full scan)
                var keys = server.Keys(pattern: $"product:category:{categoryId}:*");

                if (keys.Length > 0)
                {
                    // Delete all matching keys
                    await _redis.KeyDeleteAsync(keys.Cast<RedisKey>().ToArray());
                    Console.WriteLine($"✓ Invalidated {keys.Length} products in category {categoryId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Batch invalidation error: {ex.Message}");
            }
        }

        // ============================================================================
        // Pattern 4: Distributed Locking
        // ============================================================================
        /// <summary>
        /// Lock resource for exclusive access
        /// Prevents multiple processes from updating same resource simultaneously
        /// Lock: SET key value NX EX timeout (atomic operation)
        /// </summary>
        public async Task<bool> AcquireLockAsync(string lockKey, string lockValue, TimeSpan timeout)
        {
            try
            {
                // Try to acquire lock
                // Returns true if successful, false if already locked
                var acquired = await _redis.StringSetAsync(
                    key: lockKey,
                    value: lockValue,
                    expiry: timeout,
                    when: When.NotExists); // Only if key doesn't exist

                if (acquired)
                {
                    Console.WriteLine($"✓ Lock acquired: {lockKey}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ Lock failed: {lockKey} already locked");
                    return false;
                }
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Lock acquisition failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Release lock (only by owner)
        /// </summary>
        public async Task<bool> ReleaseLockAsync(string lockKey, string lockValue)
        {
            try
            {
                // Verify we own the lock before releasing
                var currentValue = await _redis.StringGetAsync(lockKey);

                if (currentValue == lockValue)
                {
                    // We own it - safe to delete
                    await _redis.KeyDeleteAsync(lockKey);
                    Console.WriteLine($"✓ Lock released: {lockKey}");
                    return true;
                }
                else
                {
                    // Lock expired or owned by someone else
                    Console.WriteLine($"✗ Cannot release lock - not owned by us");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lock release error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Pub/Sub Messaging
        // ============================================================================
        /// <summary>
        /// Publish event that multiple subscribers listen to
        /// Used for: Real-time notifications, cache invalidation events
        /// Message: Not persisted (fast but not reliable)
        /// </summary>
        public async Task PublishProductUpdatedEventAsync(int productId)
        {
            try
            {
                var message = JsonSerializer.Serialize(new
                {
                    ProductId = productId,
                    UpdatedAt = DateTime.UtcNow
                });

                var numSubscribers = await _subscriber.PublishAsync(
                    channel: "product:updated",
                    message: message);

                Console.WriteLine($"✓ Event published to {numSubscribers} subscribers");
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine($"Publish error: {ex.Message}");
            }
        }

        /// <summary>
        /// Subscribe to product update events
        /// </summary>
        public void SubscribeToProductUpdates(Action<int> onProductUpdated)
        {
            _subscriber.Subscribe("product:updated", (channel, message) =>
            {
                try
                {
                    var data = JsonSerializer.Deserialize<dynamic>(message.ToString());
                    var productId = (int?)data.GetProperty("ProductId") ?? 0;

                    Console.WriteLine($"✓ Received update event for product {productId}");
                    onProductUpdated?.Invoke(productId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Subscribe error: {ex.Message}");
                }
            });
        }

        // ============================================================================
        // Pattern 6: Session Storage with TTL
        // ============================================================================
        /// <summary>
        /// Store session data in Redis (faster than database)
        /// TTL: Auto-delete after 30 minutes of inactivity
        /// Use case: Web session, shopping cart, temporary data
        /// </summary>
        public async Task<bool> SetSessionAsync(string sessionId, SessionData session)
        {
            try
            {
                const string cacheKey = $"session:{sessionId}";

                var serialized = JsonSerializer.Serialize(session);

                await _redis.StringSetAsync(
                    key: cacheKey,
                    value: serialized,
                    expiry: TimeSpan.FromMinutes(30)); // Auto-delete after 30 min

                Console.WriteLine($"✓ Session stored (expires in 30 minutes)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session storage error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get session data
        /// </summary>
        public async Task<SessionData> GetSessionAsync(string sessionId)
        {
            try
            {
                const string cacheKey = $"session:{sessionId}";
                var value = await _redis.StringGetAsync(cacheKey);

                if (value.HasValue)
                {
                    // Update TTL on access (sliding window)
                    await _redis.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(30));
                    return JsonSerializer.Deserialize<SessionData>(value.ToString());
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session retrieval error: {ex.Message}");
                return null;
            }
        }

        // ============================================================================
        // Pattern 7: Incrementing Counters (Rate Limiting)
        // ============================================================================
        /// <summary>
        /// Rate limiting: Allow 1000 requests per minute per IP
        /// Counter: Incremented on each request, expires after 1 minute
        /// </summary>
        public async Task<bool> CheckRateLimitAsync(string clientId, int maxRequests = 1000)
        {
            try
            {
                const string cacheKey = $"ratelimit:{clientId}";

                // Increment counter
                var count = await _redis.StringIncrementAsync(cacheKey);

                // Set expiry only on first request
                if (count == 1)
                {
                    await _redis.KeyExpireAsync(cacheKey, TimeSpan.FromMinutes(1));
                }

                if (count <= maxRequests)
                {
                    Console.WriteLine($"✓ Request {count}/{maxRequests} for {clientId}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"✗ Rate limited: {count}/{maxRequests} requests");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Rate limit check error: {ex.Message}");
                return true; // Allow on error (fail open)
            }
        }

        // ============================================================================
        // Pattern 8: Connection Pool Configuration
        // ============================================================================
        /// <summary>
        /// Best practices for Redis connection
        /// </summary>
        public static IConnectionMultiplexer CreateRedisConnection(string connectionString)
        {
            var options = ConfigurationOptions.Parse(connectionString);

            // Connection pooling
            options.ConnectTimeout = 5000;      // 5 second timeout
            options.SyncTimeout = 5000;         // 5 second sync timeout
            options.AbortOnConnectFail = false; // Don't fail fast on connection error
            options.AllowAdmin = false;         // Security: disable admin commands

            var connection = ConnectionMultiplexer.Connect(options);

            // Verify connection
            if (!connection.IsConnected)
            {
                throw new Exception("Failed to connect to Redis");
            }

            Console.WriteLine("✓ Redis connection established");
            return connection;
        }

        // ============================================================================
        // Helper Methods (Mock Database Operations)
        // ============================================================================
        private async Task<Product> FetchProductFromDatabaseAsync(int productId)
        {
            // Simulate database query (50ms latency)
            await Task.Delay(50);
            return new Product { Id = productId, Name = $"Product {productId}", Price = 99.99m };
        }

        private async Task<bool> UpdateProductInDatabaseAsync(int productId, Product product)
        {
            // Simulate database update
            await Task.Delay(50);
            return true;
        }

        private IServer GetRedisServer()
        {
            // Get Redis server for key scanning
            var connection = ConnectionMultiplexer.Connect("localhost:6379");
            return connection.GetServer(connection.GetEndPoints().First());
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class SessionData
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public List<string> Permissions { get; set; }
        public DateTime LoginTime { get; set; }
    }
}
