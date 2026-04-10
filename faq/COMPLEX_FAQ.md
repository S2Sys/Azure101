# Complex & Advanced Azure FAQ - Expert Topics

For architects, senior developers, and those preparing for advanced technical interviews.

---

## Architecture & Design Patterns (10 Questions)

### Q1: Design a multi-region failover architecture for a mission-critical application
**Problem**: Your company's SaaS application needs 99.99% uptime across multiple regions with minimal failover time.

**Architecture**:
```
Primary Region (East US)
├─ App Service (auto-scale)
├─ SQL Database (with geo-replication to secondary)
├─ Storage (geo-redundant)
├─ Application Insights (monitoring)
└─ Front Door (global routing, failover)
            ↓ Geo-replication
Secondary Region (West US)
├─ App Service (standby, auto-scale on failover)
├─ SQL Database (read-replica, writeable on failover)
└─ Storage (read access)
```

**Implementation Details**:
- **Azure Front Door**: Smart traffic routing, health probes, instant failover
- **SQL Geo-Replication**: Asynchronous replication (RPO ~5s), manual or automatic failover
- **Storage Geo-Redundancy**: Geo-redundant storage (GRS) handles transparent failover
- **App Service**: Identical configuration in both regions

**Trade-offs**:
- **Active-Active** (both regions serve traffic):
  - ✅ Zero failover time
  - ❌ 2x infrastructure cost
  - ❌ Distributed data consistency complexity
  - ❌ Requires distributed caching
  
- **Active-Passive** (secondary standby):
  - ✅ Cheaper
  - ✅ Simpler consistency
  - ❌ 5-10 minute failover time
  - ✅ Recommended for most apps

**Monitoring & Testing**:
```
Continuous Monitoring
├─ Health checks every 30 seconds
├─ Application Insights for latency tracking
├─ Log Analytics for correlation
└─ Alerts on degradation

Monthly Failover Test
├─ Switch traffic to secondary
├─ Verify functionality
├─ Check replication lag
└─ Switch back
```

**RTO/RPO Targets**:
- RTO: < 5 minutes (Front Door fails over in seconds, warming app takes minutes)
- RPO: < 5 minutes (SQL replication lag)
- Cost: ~2x single-region (secondary region gets cheaper with Reserved Instances)

---

### Q2: Design a multi-tenant SaaS application architecture
**Problem**: Build a platform serving 1000s of customers with data isolation, multi-region support, and cost optimization.

**Architecture Patterns**:

**Option 1: Database-per-tenant (Most Secure)**
```
Azure Front Door
    ↓
API Gateway (rate limiting, auth)
    ↓
Multi-tenant App Service (stateless)
    ├─ Customer A → SQL Database A
    ├─ Customer B → SQL Database B
    ├─ Customer C → SQL Database C
    └─ Shared Storage (per tenant blob containers)
```

**Pros**: ✅ Complete isolation, ✅ Easy backup per tenant, ✅ GDPR compliance
**Cons**: ❌ Cost scales with tenant count, ❌ Complex management (many databases)

**Option 2: Schema-per-tenant (Balanced)**
```
API Gateway
    ↓
Multi-tenant App Service
    └─ Shared SQL Database (multiple schemas per customer)
        ├─ dbo.Customers
        ├─ customer_1.Orders, Payments
        ├─ customer_2.Orders, Payments
        └─ ...
```

**Pros**: ✅ Cheaper than database-per-tenant, ✅ Good isolation via schema
**Cons**: ⚠️ Schema management complexity

**Option 3: Shared Database (Cost Optimized)**
```
API Gateway → Row-Level Security
    ↓
Multi-tenant App Service
    └─ Shared SQL Database
        ├─ Customers (TenantId)
        ├─ Orders (TenantId)
        └─ Payments (TenantId)
```

**Pros**: ✅ Cheapest, ✅ Easiest management
**Cons**: ❌ Potential data leakage if RLS fails, ❌ Complex row-level security

**Recommendation**: Start with Schema-per-tenant, migrate to Database-per-tenant if isolation critical.

**Implementation**:
```csharp
// Query only current tenant's data
var orders = db.Orders.Where(o => o.TenantId == currentTenantId);

// Or use Row-Level Security in SQL
CREATE SECURITY POLICY TenantSecurityPolicy
    ADD FILTER PREDICATE fn_TenantSecurityFilter(TenantId)
    ON dbo.Orders;
```

---

### Q3: Design a scalable real-time notification system
**Problem**: Support 100M+ notifications/day, real-time delivery, high reliability.

**Architecture**:
```
Event Sources (user actions)
    ↓
Event Grid / Event Hubs (ingest, scale)
    ↓
Functions (process, enrich)
    ├─ Email (Sendgrid)
    ├─ SMS (Twilio)
    ├─ Push (Notification Hubs)
    └─ In-app (SignalR)
        ↓
    Cosmos DB (notification history, TTL auto-delete)
    
    Retry Queue (Service Bus) for failures
```

**Scaling Approach**:
- **Event Hubs**: 1000s of events/second
- **Functions**: Auto-scale from 0 to millions
- **Cosmos DB**: Partition by UserId for fast delivery lookups
- **SignalR**: Real-time in-app notifications (WebSocket)

**Failure Handling**:
```
Failed notification
    ↓
Service Bus DLQ (dead-letter queue)
    ↓
Exponential backoff retry
    ↓
After N failures, human review
```

**Cost Optimization**:
- Event Hubs (cheap, high throughput)
- Functions on consumption plan (scales to zero)
- Cosmos DB with TTL (auto-delete old notifications)

---

### Q4: Design a microservices architecture with service communication
**Problem**: Break monolithic app into 10+ microservices with async and sync communication.

**Sync Communication** (Request-Response):
```
API Gateway
    ├─ User Service (direct HTTP calls)
    ├─ Product Service
    └─ Order Service
        ├─ Calls User Service (get user details)
        └─ Calls Product Service (check inventory)
```

**Issues**: Cascading failures, tight coupling, complex error handling.

**Async Communication** (Event-Driven):
```
Order Service publishes "OrderCreated" event
    ↓
Service Bus / Event Grid
    ├─ Notification Service (send email)
    ├─ Inventory Service (reserve stock)
    ├─ Shipping Service (create shipment)
    └─ Analytics Service (log event)
```

**Benefits**: Loose coupling, resilient (services can be down), scalable.

**Hybrid Approach** (Recommended):
```
Sync for:
- Authentication/authorization
- Payment processing
- Inventory checks (must be real-time)

Async for:
- Notifications
- Analytics
- Non-critical operations
- Batch processing
```

**Service Communication Patterns**:
```
1. Direct HTTP (API calls)
   ├─ Pros: Simple
   └─ Cons: Tight coupling, cascading failures

2. Service Bus (queues/topics)
   ├─ Pros: Async, reliable delivery
   └─ Cons: Message ordering complexity

3. Event Grid (event-driven)
   ├─ Pros: Decoupled, easy integration
   └─ Cons: Not suitable for transactions

4. gRPC (high-performance RPC)
   ├─ Pros: Fast, type-safe
   └─ Cons: Complexity, tooling

5. Service Mesh (Istio/Linkerd on Kubernetes)
   ├─ Pros: Centralized traffic management
   └─ Cons: Infrastructure complexity
```

---

### Q5: Design a caching strategy for a high-traffic application
**Problem**: Database hitting capacity at peak traffic, need to reduce load 90%.

**Layered Caching**:
```
Layer 1: CDN (Azure CDN, 5-60 min TTL)
├─ Static assets (images, JS, CSS)
├─ Static content (home page, FAQs)
└─ Reduces traffic to origin 50-80%

Layer 2: Redis Cache (seconds-hours TTL)
├─ User sessions
├─ Frequently accessed data (user profile, settings)
├─ Top 1000 products (for e-commerce)
├─ Reduces database hits 90%+
└─ < 1ms latency

Layer 3: Database Query Cache
├─ Materialized views
├─ Indexed frequently filtered columns
└─ Query optimization

Layer 4: Application-level Cache
├─ In-memory cache (IMemoryCache)
├─ Local to process
└─ Careful with multi-instance apps (stale data)
```

**Implementation Strategy**:
```
User requests product details
    ↓ Check Redis
    ├─ Hit: Return cached (< 1ms)
    └─ Miss: Query database
        ├─ Get from DB
        ├─ Cache in Redis (TTL: 1 hour)
        └─ Return to user

Every night:
- Refresh popular items (top 1000 products)
- Pre-warm cache
```

**Cache Invalidation** (hardest problem):
```
Options:
1. Time-based (TTL): Simple, potential stale data
2. Event-based: On data change, invalidate cache
3. Dependency-based: Cache depends on other data
4. Versioning: Version cache keys, easy invalidation

Example: Product cache
├─ Key: "product_{id}_{version}"
├─ On product update: Increment version, cache invalidates
└─ Never actually delete, let TTL clean up
```

**Monitoring**:
```
Track:
- Cache hit rate (goal: 80%+)
- Cache miss rate (should be low)
- Redis memory usage
- Response times
```

---

### Q6: Design a data warehouse and analytics solution
**Problem**: Aggregate 100GB+ daily data into analytics platform, support complex queries.

**Architecture**:
```
Data Sources (OLTP Systems)
├─ App databases
├─ Third-party APIs
└─ Event streams
        ↓
    Azure Data Factory (ETL)
        ├─ Extract: Read from sources
        ├─ Transform: Clean, aggregate, denormalize
        └─ Load: Write to data warehouse
            ↓
    Azure Synapse Analytics (Data Warehouse)
    ├─ Fact tables (Orders, Events)
    ├─ Dimension tables (Products, Customers, Time)
    └─ Materialized views (pre-aggregated data)
            ↓
    BI Tools (Power BI, Excel)
    ├─ Dashboards
    ├─ Reports
    └─ Self-service analytics
```

**Design Pattern** (Star Schema):
```
         Fact_Orders (100M rows)
         ├─ order_id
         ├─ customer_id (FK)
         ├─ product_id (FK)
         ├─ order_date_id (FK)
         ├─ amount
         └─ quantity
              ↗   ↓   ↙
    Dim_Customers  Dim_Products  Dim_Date
    ├─ customer_id ├─ product_id ├─ date_id
    ├─ name        ├─ name       ├─ date
    └─ segment     └─ category   └─ year
```

**Key Decisions**:
1. **Batch vs. Real-time**:
   - Batch (nightly): Simpler, cheaper
   - Real-time (streaming): Higher cost, fresher data
   - Hybrid: Batch for historical, streaming for latest

2. **Star vs. Snowflake Schema**:
   - Star: Denormalized, faster queries, more storage
   - Snowflake: Normalized, less storage, more complex queries
   - Recommendation: Star for BI, snowflake if storage critical

3. **Data Marts**:
   - Separate smaller databases per department
   - Example: Sales Data Mart, HR Data Mart
   - Reduces query complexity for specific domains

---

### Q7: Design a machine learning pipeline on Azure
**Problem**: Build end-to-end ML system (data prep → model training → deployment → inference).

**Architecture**:
```
Data Collection
├─ Raw data (structured and unstructured)
├─ Data Lake Storage (all raw data)
└─ Data Factory (movement and transformation)
        ↓
Data Preparation (Azure Synapse, Databricks)
├─ Cleaning
├─ Feature engineering
├─ Train/test split (70/30)
└─ Prepared data → Azure Machine Learning
        ↓
Model Training (Azure ML)
├─ Multiple algorithms
├─ Hyperparameter tuning
├─ Cross-validation
└─ Best model selected
        ↓
Model Evaluation
├─ Test on held-out data
├─ Performance metrics
├─ Bias/fairness checks
└─ Approval for deployment
        ↓
Model Registry (versioning)
├─ Model artifacts
├─ Training data reference
├─ Performance metrics
└─ Approval workflow
        ↓
Model Deployment
├─ Azure Container Instances (single request)
├─ App Service (batch scoring)
├─ Azure ML Managed Endpoint (inference)
└─ Kubernetes (high scale)
        ↓
Inference (Predictions)
├─ Real-time: REST API
├─ Batch: Scheduled jobs
└─ Streaming: Real-time inference on event stream
        ↓
Monitoring (Model Drift)
├─ Prediction quality over time
├─ Data drift (input distribution change)
├─ Model drift (performance degradation)
└─ Retrain trigger
```

**Model Serving Options**:
```
1. Batch Predictions: Azure ML batch endpoints
   ├─ Process 1000s at once
   ├─ Cheap, scheduled
   └─ Latency: minutes to hours

2. Real-time Predictions: Azure ML inference cluster
   ├─ Serve 100s req/sec
   ├─ < 100ms latency
   └─ Always-on cost

3. Serverless: Azure Functions + ML Model
   ├─ Simple models only
   ├─ Cold start: 10-30s
   └─ Good for infrequent calls
```

---

### Q8: Design a hybrid cloud architecture (Azure + On-Premises)
**Problem**: Company has on-premises datacenter, want to extend to Azure gradually.

**Connectivity Options**:
```
1. VPN Gateway (Encrypted, internet-based)
   ├─ Bandwidth: 500Mbps - 1.3Gbps
   ├─ Latency: 30-50ms
   ├─ Cost: Cheap
   └─ Use: Dev/test, low-bandwidth

2. ExpressRoute (Private, dedicated circuit)
   ├─ Bandwidth: 50Mbps - 100Gbps
   ├─ Latency: < 5ms (consistent)
   ├─ Cost: Expensive ($50-1000+/month)
   └─ Use: Production, high-bandwidth, sensitive data

3. Azure Arc (Extend Azure management on-premises)
   ├─ Manage on-prem servers from Azure Portal
   ├─ Deploy Azure services (SQL, Arc-enabled servers)
   └─ Unified governance and compliance
```

**Hybrid Architecture**:
```
On-Premises                 Azure
├─ Data Center          ├─ Web App Service
├─ Legacy databases     ├─ New databases
├─ Active Directory     ├─ Extend AD via Azure AD Connect
└─ Business apps

Connected via:
├─ ExpressRoute (production workloads)
├─ VPN (backup, dev/test)
└─ Azure Arc (management plane)

Data Sync:
├─ Data Factory (ETL between on-premises and Azure)
├─ Azure Database Migration Service (one-time migration)
└─ SQL replication (continuous sync)
```

**Gradual Migration Strategy**:
```
Phase 1: Infrastructure as Code
├─ Define Azure resources in Bicep/Terraform
└─ Practice deployments

Phase 2: Proof of Concept
├─ Migrate one non-critical workload
├─ Test connectivity, performance
└─ Learn and iterate

Phase 3: Critical Workloads
├─ Establish ExpressRoute
├─ Migrate databases with replication
├─ High availability setup

Phase 4: Optimize
├─ Right-size resources
├─ Implement cost optimization
└─ Plan retirement of on-premises
```

---

### Q9: Design a secure, compliant system (HIPAA, PCI-DSS, GDPR)
**Problem**: Build healthcare system requiring HIPAA compliance, PCI for payments, GDPR for EU users.

**Compliance Architecture**:
```
Compliance Requirements          Azure Solutions
├─ Data encryption at rest    ├─ Azure Storage encryption
├─ Encryption in transit      ├─ TLS 1.2+ everywhere
├─ Access control            ├─ RBAC + Managed Identity
├─ Audit logging             ├─ Azure Audit Logs, Log Analytics
├─ Data residency            ├─ Region-specific deployments
└─ Incident response         └─ Azure Security Center

HIPAA-Specific:
├─ Patient data encryption     ✅ SQL TDE
├─ Audit trails (2+ years)     ✅ Azure Monitor, 90-day default
├─ Business Associate Agreement ✅ Azure BAA available
├─ Breach notification (60 days) ✅ Security Center alerts
└─ Regular vulnerability scans  ✅ Azure Defender

PCI-DSS (Credit Cards):
├─ No plaintext storage        ✅ HSM in Key Vault
├─ Encrypted transmission      ✅ TLS required
├─ Access control              ✅ RBAC
├─ Regular security testing    ✅ Penetration testing allowed
└─ Firewall configuration      ✅ NSG, Azure Firewall

GDPR (EU Data):
├─ Right to access             ✅ Data export APIs
├─ Right to deletion           ✅ Soft delete, purge options
├─ Data portability            ✅ Export to standard formats
├─ Privacy by design           ✅ Minimal data collection
└─ Data Processing Agreement   ✅ Azure DPA available
```

**Sensitive Data Handling**:
```
1. Data Minimization
   ├─ Collect only needed data
   ├─ Pseudonymize where possible
   └─ Regular audit of retention

2. Encryption
   ├─ At Rest: Azure Storage encryption, SQL TDE
   ├─ In Transit: TLS 1.2+ required
   └─ In Use: Never log sensitive data

3. Access Control
   ├─ Principle of least privilege
   ├─ MFA for all admin access
   ├─ Regular access reviews
   └─ Automatic account disabling

4. Monitoring
   ├─ Log all data access
   ├─ Alert on unusual patterns
   ├─ Regular security audits
   └─ Penetration testing
```

---

### Q10: Design an event-driven architecture with saga pattern
**Problem**: Distributed transaction across 3 services (Order, Payment, Inventory) with eventual consistency.

**Problem with Direct Transactions**:
```
Order Service calls Payment Service
                   ↓ (timeout)
                Payment Service dies
                   ↓
Order created but payment failed
→ Data inconsistency
```

**Saga Pattern Solution**:
```
Order Service: CreateOrder
    ├─ Create order (status: pending)
    └─ Publish "OrderCreated" event

Payment Service: ProcessPayment
    ├─ Listen to "OrderCreated"
    ├─ Charge customer
    ├─ If success: Publish "PaymentSucceeded"
    └─ If fail: Publish "PaymentFailed"

Inventory Service: ReserveInventory
    ├─ Listen to "PaymentSucceeded"
    ├─ Reserve stock
    ├─ If success: Publish "InventoryReserved"
    └─ If fail: Publish "InventoryReservationFailed" (trigger refund)

Notification Service:
    ├─ Listen to all completion events
    └─ Send customer notifications

Order Service: UpdateOrder
    ├─ Listen to "InventoryReserved"
    ├─ Update order status to "Confirmed"
    └─ Publish "OrderConfirmed"
```

**Compensating Transactions** (Rollback):
```
If InventoryReservationFailed:
├─ Publish "RefundPayment" event
├─ Payment Service reverses charge
├─ Publish "OrderCancelled"
└─ Notify customer of cancellation
```

**Orchestration vs Choreography**:
```
Choreography (Event-driven):
├─ Each service listens to events
├─ Services are decoupled
├─ Complex flows hard to understand
├─ Distributed troubleshooting

Orchestrator Pattern:
├─ Central service (Order Orchestrator) directs flow
├─ Services don't know about each other
├─ Easier to understand flow
├─ Central point of failure (mitigate with HA)

Recommendation: Start with choreography (simpler), 
graduate to orchestrator (for complex multi-step sagas)
```

---

## Performance & Optimization (6 Questions)

### Q11: Optimize database queries that are causing 80% of database CPU
**Analysis**:
```sql
-- Find slow queries
SELECT * FROM sys.dm_exec_query_stats
ORDER BY total_worker_time DESC

-- Show execution plan
SET STATISTICS IO ON
SELECT * FROM Orders WHERE CustomerId = 123
SET STATISTICS IO OFF
```

**Common Issues & Fixes**:
```
1. Missing Index (Most Common)
   ├─ Problem: Full table scan on 100M rows
   ├─ Cost: 1000s of ms
   └─ Fix: CREATE INDEX idx_orders_customerid ON Orders(CustomerId)
           Impact: 100x speedup

2. N+1 Query Problem
   ├─ Loop through 1000 orders
   ├─ For each, query related customer (1000 queries!)
   └─ Fix: Single JOIN query (1 query)

3. Inefficient Joins
   ├─ Joining on wrong columns
   ├─ Missing join statistics
   └─ Fix: Index on join columns, update statistics

4. Subquery Optimization
   ├─ Problem: SELECT * WHERE id IN (SELECT id FROM...)
   └─ Fix: Use JOIN or EXISTS instead

5. Data Type Mismatch
   ├─ Searching varchar as int
   ├─ Causes index ignore
   └─ Fix: Ensure columns in WHERE match data type
```

**Query Optimization Strategy**:
```
For each slow query:
1. Get execution plan (estimated cost)
2. Identify missing indexes (red warning signs)
3. Add needed indexes
4. Retest execution plan
5. Test with realistic data volume
6. Monitor for query regression

Result: Often 10x-100x speedup with right indexes
```

---

### Q12: Reduce storage costs from $5000/month to $500/month
**Analysis**:
```
Current: 50TB in Hot tier = $1.15/GB × 50,000 = $57,500/month
                                              ❌ (Unrealistic, more like $2-3k)

Actual breakdown:
├─ Hot tier: 5TB ($1.15/GB) = ~$5,750
├─ Archive tier: 45TB ($0.01/GB) = ~$450
└─ Retrieval fees: ~$200 (occasional access)
```

**Cost Reduction Strategies**:
```
1. Lifecycle Policies (Biggest Impact)
   ├─ Move data to Cool tier after 30 days
   ├─ Move to Archive after 90 days
   ├─ Delete after 2 years
   └─ Typical savings: 50-70%

   Example calculation:
   ├─ 5TB in Hot × 30 days = $5,750/month
   ├─ Same data with lifecycle:
   │  ├─ Hot (30 days): $1,925
   │  ├─ Cool (60 days): $400
   │  ├─ Archive (remaining): $40
   │  └─ Total: ~$2,365/month
   └─ Savings: $3,385/month (59%)

2. Delete Unused Data (Quick Win)
   ├─ Find data older than 1 year not accessed
   ├─ Often 20-30% of total storage
   └─ Savings: 20-30%

3. Change Redundancy (Careful!)
   ├─ GRS → LRS: 50% savings (but no geo-replication)
   └─ Only if data can be recreated

4. Compression & Deduplication
   ├─ Compress old backups (70-80% compression)
   └─ Deduplicate identical files

5. Right-size Data
   ├─ Temp files, logs, old versions
   ├─ Often 30-40% of total
   └─ Automated cleanup: Delete > 90 days

Typical Result: 70% cost reduction
```

---

### Q13: Reduce database costs from $2000/month to $400/month
**Current Setup** (High cost):
```
Premium P11 (4 vCore, 1TB): $2000/month
├─ Baseline cost
├─ Includes backup storage
└─ Standard geo-replication
```

**Cost Reduction**:
```
1. Right-size (Biggest Impact)
   ├─ Analyze actual resource usage
   ├─ Current: Using only 1vCore CPU
   ├─ Downgrade to General Purpose 2 vCore: $300/month
   └─ Savings: $1,700/month

2. Disable Geo-Replication (if not needed)
   ├─ Geo-replication adds 100%
   ├─ Use local backups only
   └─ Savings: $300/month

3. Use Elastic Pool (for multiple databases)
   ├─ Share vCore across 10 databases
   ├─ Much cheaper
   └─ Savings: 40-50%

4. Move to Hyperscale (if data is large)
   ├─ More efficient storage
   ├─ Same performance
   └─ Savings: 20-30%

Final Result:
├─ Right-size: $300
├─ No geo-replication: $300
├─ Shared pool: $150
└─ Total: ~$400/month (80% reduction)
```

**Gotchas**:
- Downsizing causes brief outage (minutes)
- Test on dev first
- Monitor performance after downgrade
- Keep auto-pause enabled for dev/test

---

### Q14: Optimize Function cold starts from 3 seconds to 100ms
**Problem**: User requests times out due to cold start.

**Solutions in Priority Order**:
```
1. Premium Plan (Biggest Impact)
   ├─ Pre-warmed instances
   ├─ Cold starts: ~200ms
   ├─ Cost: ~$200/month
   └─ Best for production

2. Keep-Alive Function (Cheap workaround)
   ├─ HTTP trigger that pings function every 5 min
   ├─ Prevents cold start
   ├─ Cost: ~$2/month
   └─ Downside: Always-on consumption

3. Optimize Function Package
   ├─ Minimize dependencies
   ├─ Use assembly trimming
   ├─ Remove unused NuGet packages
   └─ Reduce startup: 20-30%

4. Use Runtime Stacks with Lower Overhead
   ├─ Go, Rust: Faster
   ├─ Node.js, Python: Medium
   ├─ .NET: Heaviest startup
   └─ May not be practical if using .NET

5. Containerize & Run on App Service
   ├─ Pre-compiled, no jit
   ├─ Always-on, no cold starts
   ├─ Higher baseline cost
   └─ Only if truly critical

Recommendation: Premium Plan + Keep-Alive = Best cost-benefit
```

---

### Q15: Eliminate database deadlocks in distributed transactions
**Problem**: Deadlock between Order and Inventory services.

**Root Cause**:
```
Thread 1: Lock Order table, then wait for Inventory table lock
Thread 2: Lock Inventory table, then wait for Order table lock
→ Deadlock!
```

**Solutions**:
```
1. Lock Ordering (Prevents Deadlock)
   ├─ Always lock tables in same order
   ├─ Order: Inventory → Order (not reverse)
   └─ Example:
       -- Always do this order
       LOCK TABLE Inventory
       LOCK TABLE Order
       -- Process
       UNLOCK

2. Read Committed Snapshot Isolation (RCSI)
   ├─ Enable in database
   ├─ Reduces lock conflicts
   └─ ALTER DATABASE MyDb SET READ_COMMITTED_SNAPSHOT ON

3. Shorter Transactions
   ├─ Less time holding locks
   ├─ Reduces contention
   └─ Don't do expensive operations inside transaction

4. Deadlock Retry Logic
   ├─ Catch deadlock error (1205)
   ├─ Exponential backoff
   ├─ Retry N times
   └─ Last resort, not solution

5. Distributed Saga Pattern
   ├─ Avoid distributed transactions
   ├─ Use event-driven orchestration
   └─ Eventual consistency (no deadlocks possible)

Recommendation: Combination of lock ordering + RCSI + saga pattern
```

---

### Q16: Scale Cosmos DB from 10,000 RU/s to 1,000,000 RU/s without data loss
**Problem**: Database hits throughput limit during Black Friday.

**Scaling Strategies**:
```
Option 1: Automatic Scaling
├─ Cosmos DB auto-scales up/down
├─ Max RU/s: Set limit (e.g., 1M)
├─ Cost: Expensive at peak
├─ Pros: No manual intervention
└─ Cons: Takes minutes to scale up

Option 2: Partition Key Design
├─ Current: Partition by UserId (uneven distribution)
├─ Problem: Hot partitions, requests throttled
├─ Solution: Composite key (UserId + Date)
├─ Result: Even distribution, better throughput

Option 3: Request Rate Limiting & Queuing
├─ Don't send all requests at once
├─ Queue requests, retry with backoff
├─ Let Cosmos DB handle pace
└─ Prevent throttling errors

Option 4: Read Replicas
├─ Replicate to read-only regions
├─ Distribute reads geographically
└─ Reduces RU consumption

Recommended Flow:
├─ Baseline: 10K RU
├─ Auto-scale enabled to 1M max
├─ Use queuing to pace requests
├─ Monitor hot partitions
└─ Adjust partition key if needed
```

---

## Deployment & DevOps (4 Questions)

### Q17: Implement blue-green deployment with zero downtime
**Architecture**:
```
                    Azure Front Door
                           ↓
              ┌────────────────────────┐
              ↓                        ↓
        Blue Environment         Green Environment
        (Current Prod)           (New Version)
        ├─ App Service           ├─ App Service
        ├─ SQL Database          ├─ SQL Database
        └─ Health: 100%          └─ Health: 100%
```

**Deployment Flow**:
```
1. New version ready
   ├─ Build & test passes
   └─ Green environment prepared

2. Deploy to Green
   ├─ Deploy new code to Green
   ├─ Run smoke tests
   ├─ Load test (ensure performance)
   └─ Warm up connections

3. Health Checks
   ├─ Front Door health probes
   ├─ If Green unhealthy, don't switch
   └─ All systems ready

4. Switch Traffic
   ├─ Front Door switches traffic from Blue → Green
   ├─ Happens in seconds (no user impact)
   └─ Blue still running (instant rollback)

5. Monitor
   ├─ Watch Green metrics
   ├─ Check error rates
   ├─ If issues: Switch back to Blue instantly
   └─ Zero downtime rollback

6. Decommission Blue (after 1 hour)
   ├─ After stability confirmed
   ├─ Turn off Blue resources
   ├─ Cost back to normal
   └─ Keep old DB backups
```

**Implementation**:
```csharp
// Azure Front Door Configuration
var frontDoor = new FrontDoorClient();
var routingRule = new RoutingRule
{
    Name = "productionRoute",
    AcceptedProtocols = new[] { "Http", "Https" },
    PatternsToMatch = new[] { "/*" },
    ForwardingProtocol = "MatchRequest",
    BackendPoolName = "prod-backend", // Switches between Blue & Green
};
```

---

### Q18: Implement GitOps pipeline (Infrastructure as Code)
**Architecture**:
```
Developer commits to git
    ↓
GitHub/Azure Repos (source of truth)
    ↓
GitHub Actions / Azure Pipeline
    ├─ Terraform/Bicep validation
    ├─ Cost estimation
    ├─ Security scanning
    ├─ Linting
    └─ If all pass: Auto-deploy
        ↓
    Azure (apply changes)
    ├─ Infrastructure matches code
    └─ Fully reproducible
```

**Benefits**:
```
✅ Infrastructure as Code (version control)
✅ Reproducible (same code = same infra)
✅ Auditable (git history = audit trail)
✅ Collaborative (PRs for review)
✅ Automated (no manual clicks)
✅ Disaster recovery (redeploy from code)
```

**Implementation** (Bicep example):
```bicep
param location string = 'eastus'
param environment string = 'prod'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: 'storage${environment}'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
  }
}

resource appService 'Microsoft.Web/sites@2021-01-15' = {
  name: 'app-${environment}'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
  }
}
```

**Pipeline** (GitHub Actions):
```yaml
name: Deploy Infrastructure
on: [push]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Validate Bicep
        run: az bicep build -f main.bicep
      - name: Security Scan
        run: checkov -f main.bicep
      - name: Deploy
        run: az deployment group create \
          --resource-group myRg \
          --template-file main.bicep
```

---

### Q19: Implement canary deployment (gradual rollout)
**Goal**: Deploy new version to 5% of users, monitor, gradually increase.

**Approach**:
```
Deployment Timeline:
├─ T0: Version 2.0 to 5% (100 users) 
│  └─ Monitor for errors
├─ T+15min: Version 2.0 to 25% (if no errors)
├─ T+30min: Version 2.0 to 50%
└─ T+45min: Version 2.0 to 100% (complete rollout)

Rollback Trigger:
├─ Error rate > 1% → Rollback to V1.0
├─ Response time > 500ms → Rollback
├─ Critical errors → Immediate rollback
└─ All metrics good → Continue rollout
```

**Implementation with Azure Pipelines**:
```yaml
stages:
  - stage: DeployCanary
    jobs:
      - deployment: CanaryDeploy
        environment: prod-5percent
        strategy:
          runOnce:
            deploy:
              - script: |
                  # Deploy to 5% traffic split
                  az containerapp update \
                    --traffic 95=revision1 5=revision2
      
      - job: MonitorCanary
        dependsOn: CanaryDeploy
        steps:
          - script: |
              # Monitor for 15 minutes
              # Check error rate, latency
              # Auto-rollback if degradation detected
```

---

### Q20: Design and implement disaster recovery plan (RTO < 1 hour, RPO < 5 min)
**Plan Components**:
```
1. Backup Strategy
   ├─ Database: Geo-redundant PITR (point-in-time recovery)
   ├─ Storage: Geo-redundant
   ├─ VMs: Snapshots daily
   ├─ Application code: Git repository (immutable)
   └─ Configuration: Infrastructure as Code

2. Failover Process
   ├─ Detection: Continuous health monitoring
   ├─ Failover decision: Automated (< 1 minute)
   ├─ Secondary region activation: < 5 minutes
   ├─ Data consistency: Geo-replication lag (< 5 min)
   └─ User notification: Automated alerts

3. RTO (Recovery Time Objective)
   ├─ Target: < 1 hour
   ├─ Achieved by:
   │  ├─ Automated failover
   │  ├─ Pre-warmed secondary
   │  └─ Infrastructure as Code
   └─ Regular failover tests

4. RPO (Recovery Point Objective)
   ├─ Target: < 5 minutes
   ├─ Achieved by:
   │  ├─ Geo-redundant replication (async)
   │  ├─ Application-level replication
   │  └─ Transactional log shipping
   └─ Acceptable data loss: < 5 minutes
```

**Testing**:
```
Monthly Failover Test
├─ Switch to secondary region
├─ Run full smoke tests
├─ Verify RTO/RPO metrics
├─ Document issues found
└─ Switch back to primary

Quarterly Disaster Recovery Drill
├─ Full team participation
├─ Document: Time to recover, issues encountered
├─ Update runbooks based on findings
└─ Share learnings across organization
```

---

## Summary - Advanced Topics

**Architecture Decisions** are the biggest impact:
- Multi-region design (HA & DR)
- Microservices vs. monolith
- Sync vs. async communication
- Caching strategy
- Database modeling

**Performance** issues usually come from:
- Missing database indexes
- Inefficient queries
- Inadequate caching
- Under-provisioned compute
- Cold starts

**Cost** reduction opportunities:
- Right-sizing (biggest impact)
- Lifecycle policies (storage)
- Reserved instances (compute)
- Automation (reduce manual operations)

**Reliability** requires:
- Monitoring & alerting
- Automated failover
- Regular testing
- Incident playbooks
- Post-incident reviews

---

**Last Updated**: April 2026  
**Difficulty**: Advanced  
**Purpose**: Expert reference and interview preparation
