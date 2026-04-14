# .NET Backend Engineer – Microservices & Azure
## Complete Role Guide & Interview Preparation

---

## 📋 Role Overview

### Position: .NET Backend Engineer - Microservices & Azure
**Focus**: 80% feature development, 20% modernization  
**Tech Stack**: C#, .NET 6+, Azure, Docker, Kubernetes, SQL/NoSQL  
**Environment**: Production microservices, distributed systems, cloud-native

### Day-to-Day Responsibilities
- Build and maintain microservices in C#/.NET
- Write async/await code with resilience patterns
- Deploy to Azure (App Service, AKS, Functions)
- Debug production issues across services
- Optimize database queries and API performance
- Review code and mentor junior developers
- Participate in on-call rotation (1 week per month)

### Team Structure
- Squad: 6-8 engineers (frontend, backend, QA, PM)
- Backend team: 3-4 engineers
- On-call: 24/7 rotation for critical issues
- Standup: 15 minutes daily
- Sprint: 2 weeks

---

## 🎯 Core Technical Skills

### 1. Microservices & Distributed Systems

**Service Boundaries**:
- Order Service: Orders, fulfillment
- Payment Service: Credit card processing
- Inventory Service: Stock management
- Shipping Service: Logistics
- Notification Service: Email, SMS, push

**Communication Patterns**:
- Synchronous: REST, gRPC (request-response)
- Asynchronous: Service Bus, Event Hubs (fire-and-forget)
- Eventual consistency: Services may not be in sync immediately

**Distributed Transactions**:
- Saga pattern: Multi-step transactions across services
- Compensating transactions: Rollback if step fails
- Example: Order → Deduct Payment → Deduct Inventory → Create Shipment
  - If Shipment fails: Refund Payment, Restore Inventory

**Resilience Patterns**:
- Retry with exponential backoff (100ms, 200ms, 400ms, 800ms)
- Circuit breaker: Stop calling failing service, fail fast
- Timeout: Never wait indefinitely
- Bulkhead: Limit concurrent requests
- Fallback: Return cached/default value if service down

---

### 2. API Gateway & Service Communication

**API Gateway Responsibilities**:
- Single entry point for all clients
- Authentication/authorization
- Rate limiting (1000 req/min per API key)
- Request/response transformation
- Logging and monitoring
- Request routing (URL path → service)

**Service-to-Service Communication**:
- REST: Simple, stateless, widely used
- gRPC: Fast, binary protocol, low latency
- Messaging: Async, loose coupling
- Event-driven: Publish events, subscribers react

**Challenges & Solutions**:
1. Network failures → Implement retries + circuit breaker
2. Cascading failures → Timeout + fallback
3. Latency → Caching + async processing
4. Data consistency → Saga pattern for transactions

---

### 3. Azure Services Knowledge

**Compute**:
- App Service: Web apps, REST APIs
- Functions: Serverless, event-driven
- AKS: Kubernetes, microservices orchestration

**Messaging**:
- Service Bus: Reliable queuing, saga pattern
- Event Hubs: Streaming, telemetry
- Event Grid: Event routing

**Databases**:
- SQL Database: Relational, ACID
- Cosmos DB: Global scale, flexible schema
- Redis: Cache, session storage

**Monitoring**:
- Application Insights: APM (request rates, exceptions)
- Log Analytics: Centralized logging
- Azure Monitor: Infrastructure metrics

**Security**:
- Key Vault: Secrets, certificates
- Managed Identity: Passwordless auth
- RBAC: Fine-grained permissions

---

### 4. .NET Best Practices

**Async/Await**:
```csharp
// Good: Non-blocking, responsive
public async Task<Order> GetOrderAsync(int orderId)
{
    return await _database.Orders.FindAsync(orderId);
}

// Bad: Blocking, can cause deadlocks
public Order GetOrder(int orderId)
{
    return _database.Orders.Find(orderId).Result; // BLOCKS!
}
```

**Dependency Injection**:
```csharp
// Inject dependencies, don't create
public class OrderService
{
    private readonly IPaymentService _payment;
    
    public OrderService(IPaymentService payment) => _payment = payment;
}
```

**Exception Handling**:
```csharp
try {
    await _paymentService.ProcessAsync(order);
}
catch (PaymentFailedException ex)
{
    // Specific exception, log and retry
    _logger.LogWarning($"Payment failed: {ex.Message}");
    throw; // Let caller decide
}
catch (Exception ex)
{
    // Unexpected exception
    _logger.LogError($"Unexpected error: {ex}");
    throw new OrderProcessingException("Order failed", ex);
}
```

---

## 🔧 Architecture Patterns

### Pattern 1: Resilient HTTP Client
```csharp
// Retry + Circuit Breaker + Timeout
var policy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => !r.IsSuccessStatusCode)
    .FallbackAsync<HttpResponseMessage>(async _ => 
        new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
    .WrapAsync(Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(10)))
    .WrapAsync(Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(5)));

var response = await policy.ExecuteAsync(async () =>
    await _httpClient.GetAsync("https://api.service.com/data"));
```

### Pattern 2: Idempotent Message Processing
```csharp
// Track processed messages, ignore duplicates
public async Task ProcessOrderAsync(Order order)
{
    var key = $"order-{order.Id}";
    
    // Check if already processed
    if (await _cache.GetAsync(key) != null)
        return; // Already processed, skip
    
    // Process order
    await _paymentService.ProcessAsync(order);
    
    // Mark as processed
    await _cache.SetAsync(key, true, TimeSpan.FromHours(24));
}
```

### Pattern 3: Saga Pattern (Distributed Transaction)
```csharp
public async Task ProcessOrderSagaAsync(Order order)
{
    // Step 1: Deduct payment
    var payment = await _paymentService.ProcessAsync(order.Amount);
    
    try
    {
        // Step 2: Deduct inventory
        await _inventoryService.DeductAsync(order.Items);
        
        // Step 3: Create shipment
        var shipment = await _shippingService.CreateAsync(order);
        
        order.Status = OrderStatus.Shipped;
    }
    catch
    {
        // Compensating transaction: Refund payment
        await _paymentService.RefundAsync(payment);
        order.Status = OrderStatus.Failed;
        throw;
    }
}
```

---

## 🚨 Production Troubleshooting

### Scenario 1: Service A → Service B Timeout
**Diagnosis**:
1. Check Application Insights: Is Service B slow?
2. Check Azure Monitor: Is Service B CPU high?
3. Check Log Analytics: Are there errors in Service B?
4. Check database: Is query slow?

**Solutions**:
- Increase timeout (if temporary issue)
- Add caching
- Optimize database query (add index)
- Scale up Service B

### Scenario 2: Messages Stuck in Dead-Letter Queue
**Diagnosis**:
1. Check message: What's the error?
2. Is it data error (can't fix) or service error (will retry)?

**Solutions**:
- Data error: Fix data, resubmit message
- Service error: Fix service, resubmit message

### Scenario 3: High Database Latency
**Check**:
- Slow queries (Query Store)
- Missing indexes
- Connection pool exhausted
- Database CPU high

**Fix**:
- Add index
- Increase connections
- Optimize query
- Scale database

### Scenario 4: Cascading Failure
**Pattern**: Service A fails → Service B times out → Service C fails → Cascade

**Solution**: Implement timeout + circuit breaker
- Fast fail (don't wait)
- Return fallback/cached value
- Prevent cascade

---

## 📚 Interview Questions & Answers

### Microservices & Architecture (5 Questions)

**Q1: Describe a microservice architecture you've built**
A: Order → Payment → Inventory → Shipping. Each service independent database. Communicate via Service Bus. Implemented saga pattern for distributed transactions.

**Q2: Service in production failed, what happened?**
A: Database connection pooled exhausted. Increased pool size from 20 to 50. Deployed. Latency dropped from 5s to 100ms. Lessons: Monitor connection usage, adjust proactively.

**Q3: How have you handled eventual consistency?**
A: Acknowledged data lag (< 1 second). Used cache for reads. Implemented cache invalidation on write. Dashboard refreshed every 5 seconds.

**Q4: Database migrations in microservices?**
A: Blue-green deployments. Old code supports new schema (backward compat). Deploy code first. Migrate database. Deploy cutover. Rollback: Code handles old schema.

**Q5: Debugging issues across services?**
A: Used correlation IDs (trace request across services). Application Insights to see full request path. Identified slow service. Optimized query, fixed issue.

### Database & Caching (5 Questions)

**Q1: Slow query optimization?**
A: Added index on WHERE clause column. 30-second query → 100-millisecond. Used Query Store to find slowest queries.

**Q2: Caching strategy?**
A: Cache-aside pattern. Check Redis first. If miss: query database, cache for 1 hour. Invalidate on write. Hit rate: 95%.

**Q3: Database per service vs shared?**
A: Database per service. Benefits: Independent scaling, loose coupling. Challenges: Joins require app-level logic, distributed transactions.

**Q4: Database scaling challenges?**
A: Vertical scaling (bigger machine) hits limit. Used sharding by customer ID. Each shard independent database. Queries distributed across shards.

**Q5: Performance monitoring?**
A: Application Insights: track response time percentiles (p50, p95, p99). Alert if p95 > 500ms. Log slow queries (> 1 second).

### Cloud & Deployment (5 Questions)

**Q1: Experience deploying to Azure?**
A: Deployed .NET microservices to AKS. Docker images to Container Registry. Helm charts for deployment. CI/CD with Azure Pipelines. Auto-scaling based on CPU.

**Q2: Secrets and configuration?**
A: Secrets in Key Vault (never hardcode). Configuration in App Configuration. Managed Identity for authentication. Audit logged.

**Q3: Deployment failures?**
A: Health check failed → rolled back. Learned: Test rolling update before prod. Use canary deployment (10% → 50% → 100%).

**Q4: Scaling approach?**
A: Horizontal scaling (more instances). Load balancer distributes traffic. Auto-scale: CPU > 80% → add instance. CPU < 20% → remove instance.

**Q5: Monitoring strategy?**
A: Application Insights (app metrics). Log Analytics (logs). Azure Monitor (infrastructure). Alerts for critical issues. Dashboards for visibility.

### .NET Specific (5 Questions)

**Q1: Large project structure?**
A: Services folder (logical groupings). Models folder (DTOs, domain objects). Controllers folder (endpoints). Shared library (shared code). Tests folder.

**Q2: Async/await in production?**
A: Extensive async/await. All I/O operations non-blocking. Improved throughput 10x. No deadlocks with ConfigureAwait(false).

**Q3: Performance issue fixed?**
A: Discovered N+1 query (loop with database call). Changed to batch query. 1000 queries → 5 queries. Performance improved 100x.

**Q4: Exception handling?**
A: Catch specific exceptions. Log context (parameters, state). Throw custom exceptions. Middleware catches unhandled. All logged with stack trace.

**Q5: Biggest mistake & lesson?**
A: Hardcoded database password in code. Committed to GitHub. Realized: Always use Key Vault. Never commit secrets. Set up secret scanning in CI/CD.

---

## 💼 Expected First Day & Week

### Day 1:
- [ ] IDE configured (Visual Studio)
- [ ] Git access working
- [ ] Can clone main repository
- [ ] Local environment running (Docker, SQL, Redis)
- [ ] Can run tests (unit, integration)
- [ ] Attended standup

### Week 1:
- [ ] Understand service architecture
- [ ] Deploy existing change to staging
- [ ] Debug a service locally
- [ ] Made small bug fix and deployed
- [ ] Know how to check logs in production
- [ ] Met team members
- [ ] Attended planning meeting

---

## 📊 Key Metrics You'll Track

**Application Performance**:
- Request rate (requests/sec)
- Response time (p50, p95, p99)
- Error rate (%)
- Availability (%)

**System Health**:
- CPU usage (%)
- Memory usage (%)
- Disk usage (%)
- Network latency (ms)

**Business Metrics**:
- Orders processed/hour
- Payment success rate (%)
- Customer satisfaction
- Feature adoption (%)

---

**This guide prepares you for success in a .NET Backend Engineer role focused on microservices and distributed systems!** 🚀
