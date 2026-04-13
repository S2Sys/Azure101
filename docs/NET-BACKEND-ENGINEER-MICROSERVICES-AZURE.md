# .NET Backend Engineer – Microservices & Azure
## Production-Ready Interview & Role Guide

A comprehensive guide for .NET developers building and maintaining microservices in production Azure environments.

---

## 📋 Role Overview

**Position**: .NET Backend Engineer – Microservices & Azure  
**Company**: Collabera (Large Retail Client Modernization)  
**Focus**: 80% Feature Development, 20% Modernization  
**Environment**: Production microservices, distributed systems, Azure cloud  
**Team**: Agile/Scrum (standups, sprints, retros)  

---

## 🎯 Key Responsibilities

### 1. Feature Development (80%)
- Develop backend features following product roadmap
- Build RESTful APIs within microservices architecture
- Write clean, testable, maintainable code
- Participate in code reviews
- Fix bugs and performance issues

### 2. Microservices Architecture (20%)
- Modernize legacy systems to microservices
- Design service boundaries
- Implement cross-service communication
- Handle distributed system challenges
- Support and troubleshoot production issues

### 3. Agile Ceremonies
- Daily standups (15-30 min)
- Sprint planning (2-4 hours per 2-week sprint)
- Sprint reviews (1-2 hours)
- Retrospectives (1 hour)
- Backlog refinement (ongoing)

---

## 📚 Core Technical Skills Required

### 1. Microservices & Distributed Systems

**What They're Actually Testing For:**
- Have you built microservices from scratch?
- Can you explain how your services communicated?
- Did you handle failures gracefully?
- Can you describe a real production incident you fixed?

**Key Concepts to Master:**
```
Service Boundaries
├─ How to split a monolith
├─ Business vs. technical boundaries
├─ Service contracts
└─ API versioning

Service Communication
├─ REST APIs (synchronous)
├─ Message queues (asynchronous)
├─ Event-driven architecture
├─ Service-to-service authentication
└─ Circuit breakers & retries

Distributed Transactions
├─ Saga pattern (orchestration vs. choreography)
├─ Eventual consistency
├─ Compensation transactions
└─ Idempotency

Data Management
├─ Database per service
├─ Data consistency across services
├─ Event sourcing
└─ CQRS (Command Query Responsibility Segregation)

Resilience Patterns
├─ Retries with exponential backoff
├─ Circuit breakers
├─ Timeouts
├─ Bulkheads
├─ Fallbacks
└─ Health checks
```

### 2. API Gateway & Service Communication

**What They're Actually Testing For:**
- Have you used API gateways in production?
- How do you route requests?
- How do you handle authentication?
- How do you manage versioning?

**Key Concepts:**
```
API Gateway Responsibilities
├─ Request routing
├─ Authentication & authorization
├─ Rate limiting & throttling
├─ Request/response transformation
├─ Load balancing
├─ API versioning
├─ Logging & monitoring
└─ Protocol translation

Service Communication Patterns
├─ Direct HTTP/REST calls
├─ Message queues (Service Bus, RabbitMQ)
├─ Event streaming (Event Hubs, Kafka)
├─ gRPC for high-performance
└─ Service mesh (Istio, Linkerd)

Challenges to Solve
├─ Network failures
├─ Cascading failures
├─ Service discovery
├─ Load balancing
├─ Request tracing
└─ Error handling
```

### 3. Azure Services

**What They're Actually Testing For:**
- Which Azure services have you used?
- How do you deploy to production?
- How do you handle monitoring & logging?
- Can you troubleshoot production issues?

**Key Azure Services:**
```
Compute
├─ App Service (web hosting)
├─ Container Instances (simple containers)
├─ Kubernetes Service (AKS)
└─ Azure Functions (serverless)

Messaging & Events
├─ Service Bus (reliable messaging)
├─ Event Hubs (event streaming)
├─ Event Grid (event routing)
└─ Queue Storage (simple queues)

Databases
├─ SQL Database (relational)
├─ Cosmos DB (NoSQL, globally distributed)
├─ PostgreSQL (open source)
└─ Cache for Redis (in-memory cache)

Security
├─ Key Vault (secrets management)
├─ Managed Identity (service authentication)
├─ App Configuration (centralized settings)
└─ Azure AD (authentication/authorization)

Monitoring & Diagnostics
├─ Application Insights (APM)
├─ Log Analytics (log aggregation)
├─ Azure Monitor (metrics)
└─ Diagnostic settings (detailed logging)

DevOps & Deployment
├─ Azure Container Registry (image storage)
├─ Azure DevOps Pipelines (CI/CD)
├─ GitHub Actions (CI/CD)
└─ Infrastructure as Code (Bicep, Terraform)
```

---

## 🏛️ Architecture Patterns for Microservices

### Pattern 1: Service-to-Service Communication (Synchronous)

**Use Case**: Order Service needs immediate user info from User Service

```
Order Service (HTTP Client)
    ↓ (REST API call)
User Service (HTTP Server)
    ↓ (Response with user data)
Back to Order Service
```

**Challenges:**
- Network failures (what if User Service is down?)
- Latency (calling another service is slower than local DB)
- Cascading failures (if User Service slow, Order Service slow)

**Solutions:**
```
1. Retry Logic (with exponential backoff)
2. Circuit Breaker (stop calling if service failing)
3. Timeout (don't wait forever)
4. Fallback (use cached data if available)
5. Load Balancer (distribute requests)
```

### Pattern 2: Event-Driven Communication (Asynchronous)

**Use Case**: Order placed → trigger notifications, inventory, shipping

```
Order Service (publishes event)
    ↓ "OrderCreated" event
Message Queue (Service Bus, Event Hubs)
    ├─ Notification Service (subscribes, sends email)
    ├─ Inventory Service (subscribes, reserves stock)
    └─ Shipping Service (subscribes, creates shipment)
```

**Advantages:**
- Loose coupling (services don't know about each other)
- Asynchronous (non-blocking)
- Scalable (consumers process at their own pace)
- Resilient (message persisted if consumer fails)

**Challenges:**
- Message ordering
- Duplicate messages (process idempotently)
- Dead-letter queues (handle poison messages)
- Eventual consistency (data not immediately consistent)

### Pattern 3: Saga Pattern (Distributed Transactions)

**Use Case**: Coordinating Order → Payment → Inventory across services

```
Scenario: Order creation requires:
1. Create order
2. Process payment
3. Reserve inventory
4. Create shipment

If any step fails, rollback all changes (compensation)
```

**Orchestration Approach** (Recommended for complex flows):
```
Order Saga Orchestrator
├─ Call Order Service (create order)
├─ Call Payment Service (charge card)
├─ Call Inventory Service (reserve stock)
├─ Call Shipping Service (create shipment)

If any fails:
├─ Payment Service: Refund payment
├─ Inventory Service: Release reservation
├─ Shipping Service: Cancel shipment
└─ Order Service: Cancel order
```

**Choreography Approach** (Event-driven):
```
Order Service: Create order, publish "OrderCreated"
    ↓
Payment Service: Subscribe, process payment, publish "PaymentProcessed"
    ↓
Inventory Service: Subscribe, reserve stock, publish "StockReserved"
    ↓
Shipping Service: Subscribe, create shipment, publish "ShipmentCreated"

If error: Publish "OrderFailed" → compensation handlers
```

### Pattern 4: API Gateway Pattern

**Use Case**: Single entry point for all client requests

```
Mobile App/Web Client
    ↓
API Gateway (Azure Application Gateway / API Management)
    ├─ Routing: /api/users → User Service
    ├─ Routing: /api/orders → Order Service
    ├─ Routing: /api/products → Product Service
    ├─ Authentication: Verify JWT token
    ├─ Rate limiting: Max 1000 req/min per user
    ├─ Logging: All requests to Log Analytics
    └─ Transformation: Add authentication header to downstream calls
```

**Responsibilities:**
1. Request Routing
2. Authentication/Authorization
3. Rate Limiting
4. Request/Response Transformation
5. Load Balancing
6. Logging & Monitoring
7. API Versioning
8. Cache management

---

## 🔧 Production Troubleshooting Guide

### Scenario 1: Service A calling Service B times out intermittently

**Diagnosis Process:**
```
1. Check Application Insights
   ├─ What's the response time trend?
   ├─ Is it getting slower over time?
   └─ Are specific endpoints affected?

2. Check Service B logs
   ├─ Is Service B actually receiving requests?
   ├─ What's the processing time?
   ├─ Any errors?

3. Check network/infrastructure
   ├─ Is Service B healthy?
   ├─ CPU/memory usage?
   ├─ Any network issues?

4. Check Service A's retry logic
   ├─ Are retries happening?
   ├─ Exponential backoff configured?
   ├─ Circuit breaker triggered?
```

**Common Causes & Solutions:**
```
1. Service B slow (database query, external API)
   └─ Optimize queries, add caching, scale Service B

2. Network latency (cross-region calls)
   └─ Use local replicas, cache, or async pattern

3. Connection pool exhausted
   └─ Increase pool size, implement connection pooling

4. Cascading failures (Service B calls Service C, C is slow)
   └─ Implement circuit breaker, add timeout

5. Load spikes
   └─ Implement auto-scaling, rate limiting, queue requests
```

### Scenario 2: Messages stuck in Service Bus dead-letter queue

**Diagnosis Process:**
```
1. Check Azure Portal
   ├─ How many DLQ messages?
   ├─ When did they appear?
   └─ Any pattern?

2. Check Application Insights
   ├─ What exceptions are being thrown?
   ├─ Stack traces?
   └─ Which service consuming?

3. Examine a DLQ message
   ├─ What's in the message?
   ├─ Can I reproduce the error?
   └─ Is it a poison message or service issue?

4. Check service logs
   ├─ Why is processing failing?
   ├─ Is it a bug or data issue?
   └─ Need to deploy fix or handle gracefully?
```

**Common Causes:**
```
1. Poison Message (malformed JSON, missing required fields)
   └─ Fix: Parse defensively, log and skip bad messages

2. Service Bug (throws unhandled exception)
   └─ Fix: Deploy bug fix, then reprocess messages

3. External Service Unavailable (database, API)
   └─ Fix: Implement retry, check external service status

4. Message Processing Logic Error
   └─ Fix: Implement idempotency checks, handle edge cases

5. Timeout (processing takes too long)
   └─ Fix: Increase timeout, optimize processing, scale consumer
```

### Scenario 3: High database latency affecting all services

**Diagnosis Process:**
```
1. Check SQL Database metrics
   ├─ CPU usage (>80%?)
   ├─ Memory usage (>90%?)
   ├─ Deadlocks occurring?
   ├─ Long-running queries?
   └─ Connection pool exhausted?

2. Check Query Store (built-in SQL)
   ├─ Which queries using most CPU?
   ├─ Which queries have worst performance?
   └─ Have query plans changed recently?

3. Check Application Insights
   ├─ Which service making most DB calls?
   ├─ Correlation IDs for slow requests
   └─ Any N+1 query patterns?

4. Check Application Code
   ├─ Connection string configured correctly?
   ├─ Connection pooling enabled?
   ├─ Any inefficient queries?
   └─ Missing indexes?
```

**Common Causes & Solutions:**
```
1. Missing Index on WHERE clause
   └─ Solution: CREATE INDEX idx_customerId ON Orders(CustomerId)

2. N+1 Query Problem
   ├─ Problem: Loop through 100 orders, query each customer
   ├─ Result: 101 queries (1 + 100)
   └─ Solution: Single JOIN query (1 query)

3. Inefficient Query Plan
   ├─ Problem: Full table scan instead of index
   └─ Solution: Update statistics, rebuild indexes

4. High Connection Count
   ├─ Problem: Services not returning connections to pool
   └─ Solution: Use using statements, check for connection leaks

5. Database Overloaded
   ├─ Problem: More requests than database can handle
   └─ Solution: Scale up (vCore), add read replicas, implement caching

6. Deadlocks
   ├─ Problem: Transactions lock each other
   └─ Solution: Reduce lock duration, order locks consistently
```

---

## 💻 Production Code Examples

### Example 1: Resilient Service-to-Service HTTP Call

**Problem**: Calling another service can fail. Need retries, timeouts, fallback.

**Solution**:
```csharp
public interface IUserService
{
    Task<User> GetUserAsync(int userId);
}

public class ResilientUserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientUserService> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;

    public ResilientUserService(HttpClient httpClient, ILogger<ResilientUserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Retry Policy: Retry 3 times with exponential backoff
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx, 408, 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(
                    Math.Pow(2, attempt) * 100), // 100ms, 200ms, 400ms
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} after {timespan.TotalMilliseconds}ms");
                });

        // Circuit Breaker: Stop calling if 50% of last 10 requests fail
        _circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5, // 5 failures
                durationOfBreak: TimeSpan.FromSeconds(10), // Wait 10 seconds
                onBreak: (outcome, timespan) =>
                {
                    _logger.LogError($"Circuit breaker opened for {timespan.TotalSeconds}s");
                });
    }

    public async Task<User> GetUserAsync(int userId)
    {
        try
        {
            // Combine retry + circuit breaker
            var policy = Policy.WrapAsync(_retryPolicy, _circuitBreakerPolicy);

            var response = await policy.ExecuteAsync(async () =>
            {
                _logger.LogInformation($"Getting user {userId}");
                return await _httpClient.GetAsync($"/api/users/{userId}");
            });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to get user {userId}: {response.StatusCode}");
                throw new HttpRequestException($"User Service returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var user = JsonConvert.DeserializeObject<User>(content);
            return user;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open, User Service unavailable");
            // Fallback: Return cached user or default
            return await GetCachedUserAsync(userId) ?? new User { Id = userId, Name = "Unknown" };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"Failed to get user {userId}");
            throw;
        }
    }

    private async Task<User> GetCachedUserAsync(int userId)
    {
        // Return from cache or null
        return await Task.FromResult<User>(null);
    }
}

// Configuration in Startup.cs
services.AddHttpClient<IUserService, ResilientUserService>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://user-service.example.com");
        client.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout
    });
```

**Key Concepts**:
- ✅ Retry with exponential backoff (100ms, 200ms, 400ms)
- ✅ Circuit breaker (stop calling if 50% fail)
- ✅ Timeout (5 seconds max wait)
- ✅ Fallback (return cached data or default)
- ✅ Logging (track all retries and failures)

### Example 2: Processing Service Bus Messages Idempotently

**Problem**: Service Bus might deliver same message twice. Must process idempotently.

**Solution**:
```csharp
public class OrderService
{
    private readonly ServiceBusProcessor _messageProcessor;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(ServiceBusProcessor messageProcessor, 
        IOrderRepository orderRepository,
        ILogger<OrderService> logger)
    {
        _messageProcessor = messageProcessor;
        _orderRepository = orderRepository;
        _logger = logger;

        // Register message handler
        _messageProcessor.ProcessMessageAsync += ProcessMessageAsync;
        _messageProcessor.ProcessErrorAsync += ErrorHandler;
    }

    // Idempotent message processing
    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var orderEvent = JsonConvert.DeserializeObject<OrderCreatedEvent>(body);

            _logger.LogInformation($"Processing order {orderEvent.OrderId}");

            // Check if already processed (idempotency check)
            var existingOrder = await _orderRepository.GetByIdAsync(orderEvent.OrderId);
            if (existingOrder != null)
            {
                _logger.LogInformation($"Order {orderEvent.OrderId} already processed");
                await args.CompleteMessageAsync(args.CancellationToken);
                return;
            }

            // Process order
            var order = new Order
            {
                Id = orderEvent.OrderId,
                CustomerId = orderEvent.CustomerId,
                Amount = orderEvent.Amount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.CreateAsync(order);

            // Only complete message if successfully processed
            await args.CompleteMessageAsync(args.CancellationToken);

            _logger.LogInformation($"Order {orderEvent.OrderId} processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            
            // Don't complete the message, it will be retried
            // After max retries, goes to dead-letter queue
            await args.AbandonMessageAsync(args.CancellationToken);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error from Service Bus");
        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        await _messageProcessor.StartProcessingAsync();
    }

    public async Task StopAsync()
    {
        await _messageProcessor.StopProcessingAsync();
    }
}

// Configuration
services.AddSingleton(serviceProvider =>
{
    var options = new ServiceBusProcessorOptions
    {
        AutoCompleteMessages = false, // Manual completion
        MaxConcurrentCalls = 10, // Parallel processing
        ReceiveMode = ServiceBusReceiveMode.PeekLock
    };

    var client = new ServiceBusClient("connection-string");
    var processor = client.CreateProcessor("order-queue", options);
    return processor;
});
```

**Key Concepts**:
- ✅ Idempotency check (check if already processed)
- ✅ Manual message completion (only when successful)
- ✅ Error handling (abandon message on error, will retry)
- ✅ Logging (track all processing)
- ✅ Parallel processing (MaxConcurrentCalls = 10)

### Example 3: Distributed Tracing Across Services

**Problem**: Request flows through multiple services. Need to trace across all.

**Solution**:
```csharp
// Set up correlation ID (middleware or HTTP header)
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(
            CorrelationIdHeader, out var headerValue) 
                ? headerValue.ToString() 
                : Guid.NewGuid().ToString();

        context.Items["CorrelationId"] = correlationId;

        // Add to response header
        context.Response.Headers.Add(CorrelationIdHeader, correlationId);

        await _next(context);
    }
}

// Use in services
public class OrderService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderService> _logger;

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString()
            ?? Guid.NewGuid().ToString();

        _logger.LogInformation("Creating order with CorrelationId: {CorrelationId}", correlationId);

        // Pass correlation ID to downstream services
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
        httpRequest.Headers.Add("X-Correlation-Id", correlationId);
        httpRequest.Content = new StringContent(
            JsonConvert.SerializeObject(new { amount = request.Amount }),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Order created successfully with CorrelationId: {CorrelationId}", 
            correlationId);
    }
}

// Configure middleware in Startup.cs
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure Application Insights with correlation ID
services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = true;
});

// In appsettings.json
{
  "ApplicationInsights": {
    "InstrumentationKey": "your-key-here"
  }
}
```

**Key Concepts**:
- ✅ Correlation ID generated per request
- ✅ Passed through all service calls
- ✅ Logged in Application Insights
- ✅ Trace entire request flow across services

### Example 4: Database Transaction with Retry Logic

**Problem**: Database operations can fail transiently. Need retries.

**Solution**:
```csharp
public class OrderRepository
{
    private readonly IDbContextFactory<OrderContext> _contextFactory;
    private readonly ILogger<OrderRepository> _logger;

    public async Task<Order> CreateOrderAsync(Order order)
    {
        var retryCount = 0;
        const int maxRetries = 3;

        while (retryCount < maxRetries)
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                context.Orders.Add(order);
                await context.SaveChangesAsync();
                
                _logger.LogInformation($"Order {order.Id} created successfully");
                return order;
            }
            catch (DbUpdateException ex) when (IsTransientError(ex))
            {
                retryCount++;
                _logger.LogWarning($"Transient error creating order, retry {retryCount}/{maxRetries}");

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, $"Failed to create order after {maxRetries} retries");
                    throw;
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retryCount) * 100));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating order");
                throw;
            }
        }

        throw new InvalidOperationException("Failed to create order");
    }

    private bool IsTransientError(DbUpdateException ex)
    {
        // Check if error is transient (timeout, deadlock, etc.)
        var sqlException = ex.GetBaseException() as SqlException;
        if (sqlException == null) return false;

        // Transient error numbers for SQL Server
        var transientErrorNumbers = new[] { -2, 18456, 64 };
        return sqlException.Errors.Cast<SqlError>()
            .Any(e => transientErrorNumbers.Contains(e.Number));
    }
}
```

**Key Concepts**:
- ✅ Retry logic with exponential backoff
- ✅ Detect transient errors
- ✅ Don't retry on permanent errors
- ✅ Logging for diagnostics

---

## ❓ Interview Questions for This Role

### Microservices & Architecture (15 Questions)

**Q1: Describe a microservice architecture you've built in production. What services did you have?**
*Expected Answer*: 
- Name 3-5 services (Order Service, User Service, Product Service, etc.)
- Explain service boundaries (Order Service owns orders, User Service owns users)
- How they communicated (REST, messaging, etc.)
- Real challenges you faced and how you solved them

**Q2: Tell me about a time a service you wrote failed in production. What happened?**
*Expected Answer*:
- Specific incident (e.g., "Memory leak caused OOM exception")
- How you discovered it (logs, monitoring, customer complaints)
- Root cause analysis
- How you fixed it
- How you prevented it in future

**Q3: How have you handled eventual consistency in a distributed system?**
*Expected Answer*:
- Example: Order placed, then inventory reserved asynchronously
- Challenges (data temporarily inconsistent)
- Solutions (idempotent processing, compensation, timeouts)
- Real scenario from your experience

**Q4: Describe your experience with database migrations in microservices.**
*Expected Answer*:
- Challenges: Can't lock 10 tables anymore
- Solutions: Expand/contract pattern, feature flags
- Blue-green deployment
- Data consistency during migration
- Real migration you've done

**Q5: How do you debug an issue affecting multiple services?**
*Expected Answer*:
- Start with Application Insights/monitoring
- Check correlation IDs in logs
- Trace request through each service
- Check service dependencies
- Look for timeouts, retries, circuit breakers
- Real example you've solved

### Database & Caching (5 Questions)

**Q6: Tell me about a time you optimized a slow query in production.**
*Expected Answer*:
- Specific query (with example)
- How you identified it was slow (monitoring, customer complaint)
- Root cause (missing index, N+1 problem)
- Solution implemented
- Performance improvement (was it 10x faster?)
- How you prevent regressions

**Q7: How do you handle caching in a microservices environment?**
*Expected Answer*:
- Caching layers: Redis (distributed), in-memory (local)
- Cache invalidation strategies
- Stale data handling
- Cache warming
- Real scenario from your experience

**Q8: Database per service vs. shared database. What's your experience?**
*Expected Answer*:
- Pros/cons of each approach
- Your preference and why
- Challenges with database per service (distributed transactions)
- How to handle consistency
- Real architecture you've used

**Q9: What's the biggest database scaling challenge you've faced?**
*Expected Answer*:
- Was it growth in data size or request volume?
- Solutions: Caching, read replicas, indexing, partitioning
- How you monitored performance
- Real numbers (how many requests/sec, how much data)

**Q10: How do you monitor database performance in production?**
*Expected Answer*:
- Tools: Azure SQL Insights, Query Store, Application Insights
- Key metrics: CPU, memory, slow queries, deadlocks
- Alerting strategy
- Response time (alert if > 500ms)
- Real example of issue you caught with monitoring

### Cloud & Deployment (5 Questions)

**Q11: Describe your experience deploying microservices to Azure.**
*Expected Answer*:
- Compute option: App Service, AKS, or Functions
- Container strategy if using containers
- CI/CD pipeline (Azure DevOps or GitHub Actions)
- Deployment frequency (daily? hourly?)
- Real production environment you've managed

**Q12: How do you handle secrets and configuration in microservices?**
*Expected Answer*:
- Key Vault for secrets (connection strings, API keys)
- App Configuration for feature flags
- Environment-specific settings
- How you prevent secrets in code
- Rotation strategy
- Real incident if you exposed secrets (and how you fixed it)

**Q13: Tell me about an incident where deployment went wrong.**
*Expected Answer*:
- What was the issue (bug in code, config wrong, etc.)
- How you discovered it
- How you rolled back
- How you prevented similar incidents
- Real timeline and impact

**Q14: How do you approach scaling a microservice?**
*Expected Answer*:
- Vertical scaling (bigger machines)
- Horizontal scaling (more instances)
- Auto-scaling based on metrics
- When you'd choose each approach
- Stateless design for scaling
- Real example of scaling you've done

**Q15: What monitoring and alerting strategy have you implemented?**
*Expected Answer*:
- Key metrics: Response time, error rate, throughput
- Alerting thresholds (when do you page on-call engineer?)
- Tools: Application Insights, Log Analytics, dashboards
- Real alert that saved you from issues
- How you prevent alert fatigue

### .NET Specific (5 Questions)

**Q16: How do you structure a large .NET project for maintainability?**
*Expected Answer*:
- Project organization (Folder structure)
- Design patterns (Repository, Dependency Injection, etc.)
- Testing approach (unit, integration, end-to-end)
- Code reviews
- Real project you've organized

**Q17: How do you handle async/await in production services?**
*Expected Answer*:
- Why async is important (scalability)
- Common mistakes (blocking on async code, deadlocks)
- ConfigureAwait(false) when
- Real issue you debugged involving async
- Performance impact of async

**Q18: Tell me about a performance issue you fixed in .NET code.**
*Expected Answer*:
- What was slow (API endpoint, background job, etc.)
- Root cause (memory leak, inefficient algorithm, N+1)
- How you profiled it (tools: dotTrace, Application Insights)
- Solution and impact
- Real numbers (was it 10x faster?)

**Q19: How do you handle exceptions in microservices?**
*Expected Answer*:
- Global exception handler
- Logging (what gets logged)
- User-facing vs. internal errors
- Retry logic on specific exceptions
- Dead-letter queue for poison messages
- Real error handling code you've written

**Q20: What's the biggest .NET/Azure mistake you've made and learned from?**
*Expected Answer*:
- Real incident (not theoretical)
- What you did wrong
- Why it was wrong
- How you fixed it
- How you prevented future similar issues
- What you learned

---

## 🎯 What They're Really Looking For

### Technical Competence
✅ Built microservices in production (not just samples)  
✅ Debugged real production issues  
✅ Understood distributed system challenges  
✅ Hands-on experience with Azure services  

### Communication
✅ Can explain complex concepts clearly  
✅ Can describe real incidents and lessons learned  
✅ Can discuss trade-offs and decisions  

### Problem-Solving
✅ When face issue, how do you diagnose it?  
✅ How do you find root cause?  
✅ How do you prevent regressions?  

### Production Mindset
✅ Think about monitoring and alerting  
✅ Think about failure scenarios  
✅ Think about scaling challenges  
✅ Think about costs  

---

## 📋 Interview Preparation Checklist

### Before Interview
- [ ] Review your recent production incidents
- [ ] Prepare 2-3 stories about real production issues you solved
- [ ] Know the team's tech stack (ask recruiter)
- [ ] Review microservices patterns
- [ ] Brush up on .NET async/await
- [ ] Know your Azure services (which ones have you used?)
- [ ] Prepare questions to ask about the role

### During Interview
- [ ] Listen carefully to the question
- [ ] Provide specific examples (not theory)
- [ ] Mention real challenges you faced
- [ ] Discuss how you solved them
- [ ] Highlight monitoring/observability
- [ ] Show you think about failures
- [ ] Ask clarifying questions

### Questions to Ask Them
- What's the current architecture? (How many services?)
- What's been the biggest challenge? (Migrations? Scale?)
- How do they do deployments? (Frequency, rollback strategy?)
- Monitoring and observing approach?
- On-call rotation? (How often?)
- Team size? (How many people you'd work with?)
- What's the most common type of issue they debug?

---

## 🚀 Day One on the Job

### You'll Likely:
1. Get access to dev/staging environment
2. Read documentation about existing services
3. Set up local development environment
4. Make small bug fix or small feature
5. Deploy to staging (to learn deployment process)
6. Get onboarded to monitoring/alerting tools
7. Be assigned to an on-call rotation after ramp-up

### First Week Focus:
- Understand service architecture
- Know how services communicate
- Set up local debugging
- Deploy something small
- Get familiar with monitoring tools
- Meet the team

### First Month Goals:
- Fix 3-5 bugs in production
- Understand the legacy system you're modernizing
- Deploy a small feature
- Be on-call or shadowing on-call engineer
- Understand common production issues

---

## 📚 Essential Reading

### Books
- "Building Microservices" by Sam Newman
- "Release It!" by Michael Nygard (resilience patterns)
- "Designing Data-Intensive Applications" by Martin Kleppmann

### Blog Posts / Articles
- Microsoft: Microservices architecture on Azure
- AWS: Service resilience patterns
- Martin Fowler: Microservices patterns

### Documentation
- Azure documentation (services you'll use)
- .NET documentation
- Application Insights documentation

---

## 💡 Real-World Example

### Hypothetical Situation
"Your company has Order Service, Payment Service, and Inventory Service. Customer places an order. Payment processes, but then Inventory Service is down. Money charged, but order not processed. What do you do?"

**Good Answer**:
- Use idempotent payment processing (can charge safely twice, deduped)
- Implement saga pattern (compensation transaction)
- Ensure all three services eventually consistent
- Set up dead-letter queue for failed orders
- Implement monitoring to detect this
- Have runbook for manual recovery
- Use circuit breaker so Payment Service doesn't keep calling failing Inventory

---

**Last Updated**: April 2026  
**Role Focus**: Production Microservices, .NET Backend  
**Difficulty**: Intermediate to Advanced  
**Purpose**: Interview preparation and role understanding
