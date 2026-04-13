# Azure Databases & Data Services - Complete Wiki

## Overview

Azure Databases provides relational, NoSQL, and specialized data services. Choose based on data model, consistency requirements, and scale needs.

---

## Quick Comparison Table

| Service | Type | Scale | Consistency | Cost | Best For |
|---------|------|-------|-------------|------|----------|
| **SQL Database** | Relational | Multi-TB | Strong | Medium | Traditional apps, ACID |
| **Cosmos DB** | NoSQL | Unlimited | Tunable | High | Global scale, flexible schema |
| **PostgreSQL** | Relational | 16TB max | Strong | Low | Open-source, JSON, PostGIS |
| **MySQL** | Relational | Large | Strong | Low | Web apps, WordPress |
| **MariaDB** | Relational | Large | Strong | Low | MySQL-compatible alternative |
| **Redis** | Cache | 120GB max | Eventual | Low | Session cache, real-time |
| **Data Factory** | ETL | Unlimited | N/A | Pay-per-run | Data movement & transformation |

---

## 1. Azure SQL Database

### What is it?
Managed relational database (MSSQL) with automated backups, patching, and high availability.

### When to Use
- Traditional business applications
- Strong ACID transactions required
- Complex queries and reporting
- Existing SQL Server applications
- Regulatory compliance (HIPAA, PCI-DSS)

### Key Features
- **Service Tiers**:
  - Basic: Single-threaded, 2GB, ~$5/month
  - Standard: Multi-threaded, 250GB, ~$30/month
  - Premium: High performance, 1TB, ~$300/month
  - Hyperscale: 100TB+, pay-per-usage
  
- **Automatic Backups**: 7-35 days retention
- **Geo-Replication**: Read replicas in other regions
- **Elastic Pools**: Share resources across databases
- **Built-in Intelligence**: Query optimization recommendations

### Architecture & Core Concepts

#### Deployment Models
```
Single Database (recommended)
    ├── Simplest to manage
    ├── Pay per database
    └── Resource isolation

Elastic Pools
    ├── Multiple databases share resources
    ├── Better utilization
    └── Cost optimization for many databases

Managed Instance
    ├── Full SQL Server compatibility
    ├── More operational overhead
    └── Higher cost
```

#### Backup & Recovery Strategy
```
Recent Data (0-7 days)
    ├── Point-in-time restore
    └── Full transaction log
     ↓
Geo-Replicated Backup (7-35 days)
    ├── Disaster recovery
    └── Compliance retention
     ↓
Long-term Retention (up to 10 years)
    └── Archive storage (cheaper)
```

### Pros & Cons

**Pros** ✅
- Fully managed (no patching/updates)
- Strong ACID transactions
- Advanced query optimizer
- Excellent disaster recovery options
- Highly available (99.99% SLA)
- Works with existing SQL Server tools

**Cons** ❌
- Higher cost than open-source options
- Not as unlimited scale as Cosmos DB
- Connection pooling required at scale
- Premium tier expensive for small workloads
- T-SQL dialect limitations vs full SQL Server

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Database size | 4TB (single), 100TB (Hyperscale) |
| Concurrent connections | 30,000+ depending on tier |
| Transaction log size | Up to service tier max |
| DTUs per database | 4000 (Premium) |
| Backup retention | 35 days (geo-redundant) |
| Point-in-time restore | Last 35 days |
| Long-term retention | Up to 10 years |

### Real-World Use Cases

#### Use Case 1: SaaS Multi-Tenant Application
```
Elastic Pool (10 databases)
├── Tenant 1 Database
├── Tenant 2 Database
├── ...
└── Tenant 10 Database
    └── Share: 10 DTUs total
    (Cost: ~$200/month vs ~$300 individual)
```

#### Use Case 2: High-Transaction Volume
```
Application Tier
    ├── Read replicas (query load)
    ├── Write primary (transactions)
    └── Geo-backup (disaster recovery)
    
Result: 1000 writes/sec, 10000 reads/sec
SLA: 99.99% availability
```

#### Use Case 3: Compliance & Audit
```
SQL Database
├── Automatic daily backups (35 days)
├── Geo-redundant copies
├── Long-term retention (7 years)
├── Immutable ledger tables
└── Auditing enabled
(Meets HIPAA, PCI-DSS, SOC2 requirements)
```

### Performance Tips
- Use connection pooling (min 10-50 connections)
- Enable index recommendations
- Monitor Query Store for slow queries
- Implement query result caching
- Use tempdb for intermediate results
- Avoid SELECT * - specify columns only

### Cost Optimization
- Use elastic pools for multiple databases
- Right-size service tier (monitor actual DTU usage)
- Use geo-redundancy only if needed
- Long-term retention instead of manual backups
- Archive old data to blob storage

---

## 2. Azure Cosmos DB

### What is it?
Globally distributed, multi-model NoSQL database with guaranteed 99.99% availability and single-digit millisecond latency.

### When to Use
- Global scale (50+ regions)
- Variable schema (semi-structured data)
- Real-time applications
- IoT/time-series data
- User profiles, catalogs
- Eventually consistent acceptable

### Key Features
- **Multi-Model Support**: Document (JSON), Key-Value, Graph, Table
- **Global Distribution**: Data replicated to 50+ regions
- **Consistency Levels**: Strong, Bounded Staleness, Session, Consistent Prefix, Eventual
- **Scaling**: Unlimited throughput and storage
- **Automatic Failover**: Multi-region writes

### Architecture & Core Concepts

#### Consistency Levels (Latency vs Freshness Tradeoff)
```
Strong Consistency
    └── Most fresh, slowest (global coordination needed)
    
Bounded Staleness
    └── Slightly stale, better latency
    
Session (Default)
    └── Your writes visible to you, others see eventual
    
Consistent Prefix
    └── Writes seen in order, others see delayed
    
Eventual Consistency
    └── Fastest, most stale (good for caching)
```

#### Capacity Modes
```
Provisioned Throughput (RUs = Request Units)
    ├── Pay per RU/second
    ├── Fixed for predictable workloads
    └── Example: 400 RU/s = $20/month

Serverless
    ├── Pay per request
    ├── Good for variable/bursty workloads
    └── Better for development/testing
```

### Pros & Cons

**Pros** ✅
- True global scale (50+ regions)
- Guaranteed latency (99th percentile < 10ms)
- Multi-consistency levels
- Automatic multi-region failover
- Flexible schema (JSON)
- No migration needed between regions

**Cons** ❌
- Significantly more expensive than SQL
- RU costs can be unpredictable
- No SQL joins (more app-side logic)
- No ACID transactions (multi-partition)
- Learning curve (NoSQL mindset)
- Hot partition problems

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Storage per partition | 20GB |
| Item size | 2MB |
| Documents per container | Unlimited |
| RU/sec per partition | 10000 |
| Throughput per account | 1,000,000 RU/sec |
| Query timeout | 5 minutes |
| Maximum partition key cardinality | Unlimited |

### Real-World Use Cases

#### Use Case 1: Global IoT Platform
```
Sensors worldwide
    └─ Data points every 10 seconds
    └─ Sent to Cosmos DB (nearest region)
    └─ Automatically replicated globally
    
Requirements:
├── Millisecond write latency (Cosmos achieves)
├── Global distribution (Cosmos handles)
└── Scale to billions of writes (Cosmos scales infinitely)
```

#### Use Case 2: Content Catalog
```
Product Catalog
├── Document: {id, name, price, tags, nested categories}
├── Flexible schema (different products have different fields)
├── Global users see consistent catalog (Strong consistency)
└── Background analytics get eventual consistency
    (Cost: ~1000 RU/sec = $400/month)
```

#### Use Case 3: Real-Time User Analytics
```
User Events: {userId, action, timestamp}
├── Write events to Cosmos
├── Eventual consistency acceptable
├── Real-time dashboards query latest data
└── Analytics pipeline processes historical data
    (Cost: Much cheaper than SQL for writes at scale)
```

### Performance Tips
- Design schema for single-partition queries
- Minimize cross-partition queries (fan-out expensive)
- Use composite indexes for common filters
- Batch writes with stored procedures
- Implement connection pooling
- Monitor consumed RUs per operation

### Cost Optimization
- Use serverless for unpredictable workloads
- Right-size throughput (monitor actual RU consumption)
- Use eventual consistency where possible (cheaper)
- Archive old data to blob storage
- Partition strategically to avoid hot partitions
- Bulk import instead of individual inserts

---

## 3. Azure Database for PostgreSQL

### What is it?
Managed PostgreSQL relational database with advanced features (JSON, PostGIS, full-text search).

### When to Use
- Open-source preference
- Advanced SQL features (JSON, arrays, ranges)
- Geospatial queries (PostGIS)
- Natural text search
- Cost-sensitive workloads

### Key Features
- **Versions**: PostgreSQL 11, 12, 13 (updated regularly)
- **Extensions**: 200+ extensions available
- **PostGIS**: Geographic and spatial data
- **Full-Text Search**: Advanced text search
- **JSON Support**: JSONB native type
- **Scaling**: Vertical scaling up to 16TB storage

### Pros & Cons

**Pros** ✅
- Rich SQL feature set
- Advanced data types (JSON, arrays, ranges)
- Excellent for analytics
- PostGIS for geospatial
- Lower cost than SQL
- Strong consistency

**Cons** ❌
- Not unlimited scale (max 16TB)
- Cannot scale horizontally easily
- Smaller ecosystem than MySQL
- Manual sharding needed for extreme scale
- Limited auto-scaling capabilities

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Storage | 16TB |
| Backup retention | 35 days |
| Maximum connections | 250-5000 depending on tier |
| Max DB size | 1TB per database (many databases possible) |
| Replication lag | < 100ms |

### Real-World Use Cases

#### Use Case 1: Geospatial E-Commerce
```
Stores with location data
├── PostGIS: Store as POINT(lat, lng)
├── Query: Find stores within 5km of user
├── Fast geospatial queries
└── Full-text search on product names
```

#### Use Case 2: Content Platform with Analytics
```
Articles with metadata
├── Title, body (text)
├── Tags (array type: ARRAY[text])
├── Metadata (JSONB: flexible nested object)
├── Full-text search on content
└── Analytics queries on tags and metadata
```

---

## 4. Azure Database for MySQL

### What is it?
Managed MySQL database - most popular open-source relational database worldwide.

### When to Use
- WordPress, Drupal, and LAMP stack apps
- Open-source preference
- Cost-sensitive workloads
- Web applications
- Avoiding proprietary licenses

### Key Features
- **Versions**: MySQL 5.7, 8.0
- **Read Replicas**: Scale reads
- **Automatic Backups**: 35 days retention
- **SSL/TLS**: Encrypted connections
- **Firewall**: VNet integration

### Pros & Cons

**Pros** ✅
- Very low cost
- Huge ecosystem (WordPress, Drupal, etc)
- Fast for read-heavy workloads
- Simple to learn and manage
- Wide hosting support

**Cons** ❌
- Not as feature-rich as PostgreSQL
- Limited JSON support (vs JSONB)
- No spatial data support
- Maximum table size 64TB (practical limits smaller)
- Community support only (not enterprise)

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Storage | 16TB |
| Connections | 300-15000 depending on tier |
| Maximum table size | 64TB (practical: much smaller) |
| Backup retention | 35 days |
| Binary log size | 1GB |

### Real-World Use Cases

#### Use Case 1: WordPress Installation
```
WordPress App
    └── MySQL Database
    ├── Posts table
    ├── Comments table
    ├── Users table
    └── Options table
    
Cost: ~$50/month (very cheap)
Performance: Good for 100k-1M users
```

---

## 5. Azure Cache for Redis

### What is it?
Managed in-memory data store for caching and real-time operations. Extremely fast (< 1ms latency).

### When to Use
- Session caching
- Database query caching
- Real-time leaderboards
- Rate limiting
- Pub/sub messaging
- Shopping carts (temporary data)

### Key Features
- **In-Memory**: Ultra-fast access (< 1ms)
- **Persistence**: Optional RDB snapshots
- **Cluster**: Horizontal scaling with clustering
- **Replication**: Master-replica for HA
- **Modules**: Additional data structures

### Pros & Cons

**Pros** ✅
- Extremely fast (< 1ms)
- Simple key-value operations
- Data structures (lists, sets, hashes)
- Pub/sub for real-time messaging
- Automatic replication

**Cons** ❌
- Limited to 120GB (practical: much smaller)
- Data loss if not persisted
- Not suitable for primary data store
- Cluster mode adds complexity
- Cost can be high for large caches

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Cache size | 120GB max |
| Keys per database | 2.5 billion |
| Key size | 512MB |
| Value size | 512MB |
| Databases | 16 |
| Connections | 7500 (C6 tier) |

### Real-World Use Cases

#### Use Case 1: Session Cache
```
User Login
    └── Create session
    └── Store in Redis
    └── Key: session-id-abc123
    └── Value: {userId, permissions, expiration}
    
Next Request
    └── Check Redis for session (1ms)
    └── Load user context
    └── No database hit needed
```

#### Use Case 2: Rate Limiting
```
API Endpoint (1000 req/sec limit)
    └── Each request increments counter in Redis
    └── Key: rate-limit-{user-id}-{minute}
    └── Expire after 60 seconds
    └── Ultra-fast checks (< 1ms)
```

#### Use Case 3: Real-Time Leaderboard
```
Game Leaderboard
├── Sorted set in Redis
├── Score as sort key
├── Update: ZADD leaderboard 1000 player1
├── Query top 10: ZRANGE leaderboard 0 9
└── Sub-millisecond updates
```

### Performance Tips
- Use pipelining for batch operations
- Implement connection pooling
- Expire keys to manage memory
- Monitor eviction policies
- Use clustering for large caches
- Implement cache aside pattern

### Cost Optimization
- Right-size cache tier
- Set TTL on keys (automatic cleanup)
- Archive old data to blob storage
- Monitor hit/miss rates
- Use serverless features if available

---

## 6. Azure SQL Managed Instance

### What is it?
Fully managed SQL Server with near-100% compatibility. More enterprise-focused than SQL Database.

### When to Use
- Existing SQL Server applications needing migration
- Cross-database transactions required
- SQL Server Agent jobs needed
- Full SQL Server compatibility essential
- Enterprise features (CDC, replication)

### Key Features
- **Full SQL Server Compatibility**: Run existing code
- **SQL Agent**: Automated jobs and backups
- **Cross-Database Transactions**: ACID across databases
- **Replication**: Native SQL Server replication
- **Linked Servers**: Connect to on-premises databases

### Pros & Cons

**Pros** ✅
- Maximum SQL Server compatibility
- Enterprise features included
- Can run complex jobs
- Works with existing tools
- VNet integration (no public endpoints by default)

**Cons** ❌
- Higher cost than SQL Database
- More operational overhead
- Longer deployment times
- Overkill for simple applications
- Not as flexible scaling

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Instance vCores | 80 vCores max |
| Storage | Up to 8TB |
| Databases per instance | 100+ |
| Backup retention | 35 days |

---

## 7. Azure Data Factory

### What is it?
Cloud-based ETL (Extract, Transform, Load) service for data movement and transformation at scale.

### When to Use
- Data pipeline orchestration
- Moving data between systems
- Scheduled data transformations
- Data lake ingestion
- Compliance/audit logging of data movement

### Key Features
- **Data Movement**: 90+ connectors to various sources
- **Transformation**: Data Flows (visual) or Mapping/Script
- **Scheduling**: Trigger pipelines on schedule or events
- **Monitoring**: Rich logging and alerts
- **Scaling**: Automatically scales for large data volumes

### Pros & Cons

**Pros** ✅
- 90+ data connectors
- No coding required (visual builder)
- Automatic scaling
- Serverless (pay-per-run)
- Great for non-technical users

**Cons** ❌
- Can be expensive for frequent runs
- Complex scenarios need custom code
- Learning curve (Azure-specific)
- No real-time processing (batch only)
- Limited error handling by default

### Real-World Use Cases

#### Use Case 1: Daily Data Warehouse Load
```
On-Premises Databases
    └─ Data Factory Pipeline (daily 2am)
    ├─ Extract: Read from OLTP DB
    ├─ Transform: Clean, aggregate, dedup
    └─ Load: Write to Azure SQL Data Warehouse
    
Cost: ~$1-5/day depending on data volume
```

#### Use Case 2: Data Lake Ingestion
```
Multiple Cloud Sources (AWS, Google, On-Prem)
    └─ Data Factory
    ├─ Read from all sources
    ├─ Standardize schemas
    └─ Write to Azure Data Lake
    
One pipeline, many sources
```

---

## Architecture Patterns

### Pattern 1: OLTP + OLAP Separation
```
OLTP (SQL Database)
├── Optimized for transactions
├── Normalized schema
└── Real-time business data
    └─ Data Factory (nightly)
        └─ Transform and aggregate
    └─ OLAP (SQL Data Warehouse)
        ├── Denormalized schema
        ├── Optimized for analytics
        └── Reports and dashboards
```

### Pattern 2: Global Cache + Primary Database
```
User Request
    └─ Check Redis Cache (1ms)
    ├─ Cache hit: return immediately
    └─ Cache miss:
        └─ Query SQL Database
        └─ Store in Redis (expire in 1 hour)
        └─ Return to user

Result: 99% cache hit rate, minimal database load
```

### Pattern 3: Data Warehouse & Lake
```
Operational Systems
├─ CRM
├─ ERP
├─ Ecommerce
    └─ Data Factory (daily ETL)
    └─ Data Lake (raw data)
    └─ SQL Data Warehouse (structured)
    └─ Power BI (reports)
```

---

## Interview Questions

1. **SQL Database vs Cosmos DB - when to use each?**
   - SQL: Strong consistency, ACID, relational schema, < 4TB
   - Cosmos: Global scale, flexible schema, eventual consistency, unlimited

2. **How would you design a high-transaction database?**
   - Use read replicas for queries
   - Connection pooling (50+ connections)
   - Sharding for extreme scale
   - Cache frequently accessed data in Redis

3. **Database design for 1 billion IoT events per day**
   - Cosmos DB (serverless) or Time Series Database
   - Partition by timestamp (or by device)
   - Archive old data to blob storage
   - Real-time stream processing with Stream Analytics

4. **Backup and disaster recovery strategy**
   - Automated daily backups (35+ days)
   - Geo-redundant replicas
   - Point-in-time restore capability
   - Regular restore tests

5. **Cost optimization for databases**
   - Right-size storage and compute
   - Archive old data
   - Use caching for reads
   - Consider managed instance vs SQL Database
   - Batch writes instead of individual

6. **Handle hot partitions in Cosmos DB**
   - Design partition key carefully
   - Avoid user-id if uneven distribution
   - Use composite keys (user-id + date)
   - Implement client-side load shifting

7. **PostgreSQL vs MySQL - differences?**
   - PostgreSQL: Rich SQL, JSON, PostGIS, analytics
   - MySQL: Simpler, lower cost, web apps, WordPress

---

## Cost Optimization Tips

1. **Choose right service**: MySQL << PostgreSQL << SQL Database << Cosmos DB
2. **Right-size tier**: Monitor actual utilization
3. **Archive old data**: Move cold data to blob storage
4. **Use Redis for caching**: Reduce database queries
5. **Implement connection pooling**: Improve throughput
6. **Batch operations**: Fewer requests = lower cost
7. **Disable backups if not needed**: Small cost savings

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Data persistence, consistency, scaling
