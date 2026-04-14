using System;
using System.Collections.Generic;

namespace Azure101.Databases.InterviewContent
{
    /// <summary>
    /// Interview Q&A for Azure Database Services
    /// SQL Database, Cosmos DB, PostgreSQL, MySQL, Redis, Data Factory
    /// </summary>
    public static class DatabasesInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            (
                "What's the difference between SQL Database and Cosmos DB?",
                @"
SQL Database (Relational, ACID):
├── Strong consistency (ACID transactions)
├── Relational schema (normalized)
├── SQL queries with joins
├── Max size: 4TB (single), 100TB (Hyperscale)
├── Scaling: Vertical (up/down), limited horizontal
├── Cost: ~$5-300/month
├── Best for: Traditional apps, complex transactions
└── Limits: Cannot scale infinitely

Cosmos DB (NoSQL, Eventually Consistent):
├── Flexible consistency levels
├── Document/key-value schema (flexible)
├── Limited querying (no complex joins)
├── Unlimited scale (global)
├── Scaling: Automatic, infinite
├── Cost: ~$20-5000+/month (usage-based)
├── Best for: Global scale, flexible schema, IoT
└── Benefits: True global distribution

Decision Matrix:
SQL Database when:
├── Data structure is well-defined
├── Complex queries with joins needed
├── ACID transactions mandatory
├── Scale < 4TB acceptable
└── Regional deployment OK

Cosmos DB when:
├── Schema varies between documents
├── Simple key-value or document queries
├── Global distribution required
├── Infinite scale needed
└── Eventual consistency acceptable
                ",
                "Database Selection"
            ),

            (
                "How would you optimize a slow query in SQL Database?",
                @"
Step 1: Identify Slow Query
├── Enable Query Store
├── Find queries with longest duration
├── Top slowest: SELECT... queries > 1 second

Step 2: Analyze Execution Plan
├── Right-click query → Display Estimated Plan
├── Look for Table Scans (red flags!)
├── Identify missing indexes (Cost % high)
├── Check join order (expensive nested loops)

Step 3: Create Missing Indexes
Example:
SELECT * FROM Orders WHERE CustomerId = 5
(Table Scan - BAD!)

Fix:
CREATE INDEX idx_orders_customer ON Orders(CustomerId)

After index:
SELECT * FROM Orders WHERE CustomerId = 5
(Index Seek - GOOD!) 10x faster

Step 4: Rewrite Query
Bad: SELECT * FROM Orders WHERE YEAR(OrderDate) = 2023
├── Function on column prevents index use
├── Table scan: 100% of rows evaluated

Better:
SELECT * FROM Orders
WHERE OrderDate >= '2023-01-01'
  AND OrderDate < '2024-01-01'
├── Index Seek: Only rows in range
├── 1000x faster!

Step 5: Eliminate N+1 Queries
Bad (from application):
for each customer:
    SELECT OrderCount FROM Orders WHERE CustomerId = customer.Id
    -- 1000 queries for 1000 customers!

Better (single query):
SELECT CustomerId, COUNT(*) as OrderCount
FROM Orders
GROUP BY CustomerId

Step 6: Add Covering Index
Bad: Index on CustomerId only
├── Index has CustomerId
├── Query needs OrderDate too
├── Must look up main table (key lookup)

Better: Covering index
CREATE INDEX idx_covering ON Orders(CustomerId)
  INCLUDE (OrderDate, TotalAmount)
├── All needed columns in index
├── No table lookup needed
└── ~5x faster

Performance Result:
Before optimization: 30 second query
After optimization: 0.5 second query (60x faster!)
                ",
                "Performance Optimization"
            ),

            (
                "Design a database schema for 1 billion IoT sensor readings",
                @"
Scenario: Temperature sensors, 1 million devices, 1 reading/10 sec

Requirements:
├── Store 1 billion+ readings efficiently
├── Query last hour readings from device X
├── Aggregate: Average temp by location, hour
└── Real-time dashboard updates

Bad Approach (Single Table):
CREATE TABLE SensorReadings (
    Id INT,
    DeviceId INT,
    Temperature FLOAT,
    Timestamp DATETIME,
    PRIMARY KEY(Id)
)
-- 1 billion rows
-- Query time: 30+ seconds
-- Storage: 50GB+
-- Cost: High

Better Approach (Partitioning):

1. Partition by Date:
CREATE TABLE SensorReadings_2024_04_01 (DeviceId, Temp, Timestamp)
CREATE TABLE SensorReadings_2024_04_02 (DeviceId, Temp, Timestamp)
...
CREATE TABLE SensorReadings_2024_12_31 (DeviceId, Temp, Timestamp)

Benefits:
├── Query only relevant partition
├── Old data can be archived
├── Parallel queries across partitions
└── Better performance (10x faster)

2. Add Indexes:
CREATE INDEX idx_device_timestamp
  ON SensorReadings_* (DeviceId, Timestamp)

3. Use Cosmos DB Instead:
Document structure:
{
  \"deviceId\": \"DEVICE-001\",
  \"timestamp\": 1234567890,
  \"temperature\": 23.5,
  \"location\": \"Building-A-Floor-3\",
  \"ttl\": 2592000  // Auto-delete after 30 days
}

Benefits:
├── Partition by deviceId (1M partitions)
├── Each device query touches 1 partition
├── Automatic archival (TTL)
└── Cost: ~$5000/month for 1B reads

Query Examples:

Get device readings (last hour):
SELECT * FROM c
WHERE c.deviceId = \"DEVICE-001\"
  AND c.timestamp > @hour_ago
LIMIT 360  // 1 reading per 10 seconds = 360 readings

Performance: 100ms (single partition)

Aggregate (avg temp by location, last hour):
SELECT
    c.location,
    AVG(c.temperature) as avg_temp,
    COUNT(*) as reading_count
FROM c
WHERE c.timestamp > @hour_ago
GROUP BY c.location

Performance: 5 seconds (all partitions in parallel)

Cost Comparison:
SQL Database:
├── 1 billion rows: 100GB storage
├── Cost: $200-500/month
├── Query: 30 seconds (slow)
└── Scaling: Manual partitioning

Cosmos DB:
├── 1 billion documents: Unlimited
├── Cost: $5000/month (RU-based)
├── Query: 100ms-5 sec (fast)
└── Scaling: Automatic

ROI: Cosmos DB is faster + easier (worth extra cost)
                ",
                "Schema Design at Scale"
            ),

            (
                "Implement caching strategy for e-commerce product catalog",
                @"
Scenario: 1M products, 100k concurrent users, 1000 req/sec

Solution: Database + Cache Tier

Layer 1: Database (Authoritative)
├── Azure SQL Database
├── Normalized schema (product, category, price)
├── Updated when admin changes product

Layer 2: Cache (Fast Access)
├── Azure Cache for Redis
├── In-memory, sub-millisecond response
└── Key: \"product-123\", Value: {id, name, price...}

Cache-Aside Pattern:

1. User requests product 123
2. Check Redis: GET \"product-123\"
   ├── Cache HIT: Return instantly (1ms)
   └── Cache MISS: Go to database

3. If cache miss:
   ├── Query SQL: SELECT * FROM Products WHERE Id = 123
   ├── Get result: {id: 123, name: \"Widget\", price: 99}
   ├── Write to Redis: SET \"product-123\" <value>
   └── Return to user (50ms total)

4. Next user requests same product:
   └── Cache HIT (from step 2): 1ms response

Benefits:
├── Hit rate: 95% (1ms response)
├── Miss rate: 5% (50ms response)
├── Average: 5% × 50ms + 95% × 1ms = 3.5ms
└── vs Database only: 50ms (14x faster!)

Invalidation Strategy:

When product updated:
1. Update database
2. Delete from cache: DEL \"product-123\"
3. Next request populates cache with new value

Bulk Invalidation (Category Change):
DEL product-*  // Delete all products from cache
-- Next requests repopulate as needed

TTL (Time-To-Live) Strategy:
SET \"product-123\" <value> EX 3600  // 1 hour TTL
└── Auto-expire if not updated

Cost Analysis:
Database only (no cache):
├── 1000 req/sec × 50ms = 50,000 DB calls/sec
├── Need 10 SQL pools to handle
├── Cost: $5000/month

Database + Redis Cache:
├── 950 cache hits/sec (sub-ms)
├── 50 DB misses/sec (re-populate cache)
├── 1 SQL pool sufficient
├── Redis: $100/month
├── SQL: $500/month
├── Total: $600/month
└── Savings: $4400/month (73% cheaper!)

Code Example:
public async Task<Product> GetProductAsync(int productId)
{
    // Check cache
    var cached = await redis.GetAsync($\"product-{productId}\");
    if (cached != null)
        return JsonConvert.DeserializeObject<Product>(cached);

    // Cache miss - query database
    var product = await db.Products.FindAsync(productId);

    // Store in cache (1 hour TTL)
    await redis.SetAsync(
        $\"product-{productId}\",
        JsonConvert.SerializeObject(product),
        TimeSpan.FromHours(1)
    );

    return product;
}
                ",
                "Caching Strategy"
            )
        };
    }
}
