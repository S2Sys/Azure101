# Azure Integration Services - Complete Wiki

## Overview

Azure Integration provides APIs, middleware, and serverless functions to connect cloud and on-premises systems. Enable modern cloud-native and hybrid architectures.

---

## Quick Comparison Table

| Service | Purpose | Type | Complexity | Cost | Best For |
|---------|---------|------|-----------|------|----------|
| **API Management** | API gateway/management | Gateway | Medium | Medium | API publishing, versioning, monetization |
| **Azure Functions** | Serverless computing | Serverless | Low-Medium | Per-execution | Event-driven, short tasks |
| **Logic Apps** | Low-code workflows | Visual workflow | Low | Per-execution | Business process automation |
| **Service Bus** | Message queue | Messaging | Medium | Per-message | Distributed systems, saga pattern |
| **Event Grid** | Event routing | Event broker | Low | Per-event | Reactive, event-driven |

---

## 1. API Management

### What is it?
Managed service for publishing, managing, and monetizing APIs. Acts as gateway between clients and backend services.

### When to Use
- Expose internal APIs to partners/public
- API versioning and backward compatibility
- Rate limiting and throttling
- API documentation
- Monetization/billing
- Analytics on API usage

### Key Features
- **API Gateway**: Central entry point
- **Developer Portal**: Self-service API discovery
- **Versioning**: Support multiple API versions
- **Rate Limiting**: Quota and throttling
- **Policies**: Modify requests/responses
- **Monetization**: Billing and subscriptions
- **Analytics**: Track API usage

### Architecture & Core Concepts

#### API Management Architecture
```
Clients
    ↓
API Gateway (APIM)
├── Authentication
├── Rate limiting
├── Request transformation
├── Caching
└── Logging
    ↓
Backend Services
├── API 1
├── API 2
└── API 3

Clients see:
├── Single unified API
├── Consistent authentication
├── Rate limiting enforced
└── Consistent versioning
```

#### Versioning Strategy
```
v1 (Legacy, deprecated)
    ├── /api/v1/users
    └── Sunset: 6 months

v2 (Current)
    ├── /api/v2/users (new fields, performance improvements)
    └── Recommended version

v3 (Development)
    ├── /api/v3/users (beta features)
    └── Not recommended yet

Backward compatibility:
    └── /users (redirects to v2)
```

### Pros & Cons

**Pros** ✅
- Centralized API management
- Multiple policies for security
- Developer portal for self-service
- API versioning simplified
- Monetization support
- Analytics on API usage
- Rate limiting built-in

**Cons** ❌
- Additional latency (gateway overhead)
- Complex configuration
- Cost can be high (per gateway unit)
- Overkill for internal-only APIs
- Learning curve for policies

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| APIs per gateway | 10000+ |
| Operations per API | 10000+ |
| Backends per gateway | 1000+ |
| Rate limit policies | Unlimited |
| API versions | Unlimited |

### Real-World Use Cases

#### Use Case 1: SaaS API Publishing
```
Internal Services
├── User Service (port 8001)
├── Order Service (port 8002)
└── Product Service (port 8003)

API Management Gateway
├── Endpoint: api.company.com
├── All APIs versioned
├── Rate limit: 1000 req/minute per customer
├── Authentication: OAuth 2.0
└── Billing per request

Customers
    ├── See single API
    ├── Self-service portal
    ├── API keys managed
    └── Billing tracked automatically
```

#### Use Case 2: Partner Integration
```
External Partners
    ├── LinkedIn (social integration)
    ├── Stripe (payments)
    └── SendGrid (email)

API Management:
    ├── Standardize authentication
    ├── Rate limit per partner
    ├── Monitor usage
    └── Implement retry logic

Result: Partners integrate through single gateway
```

---

## 2. Azure Functions

### What is it?
Serverless compute service. Write code that runs in response to events without managing infrastructure.

### When to Use
- Event-driven processing
- Scheduled tasks
- HTTP API endpoints
- Background jobs
- Data transformation
- Microservices

### Key Features
- **Triggers**: HTTP, Timer, Queue, Blob, Event Hub, etc.
- **Bindings**: Input/output connectors
- **Runtime**: C#, Python, JavaScript, Java, PowerShell
- **Scalability**: Auto-scale to thousands of instances
- **Pricing**: Pay per execution + duration

### Architecture & Core Concepts

#### Triggers (What Starts the Function)
```
HTTP Trigger
    ├── Triggered by HTTP request
    ├── Response returned to caller
    └── Good for: APIs, webhooks

Timer Trigger
    ├── Triggered on schedule (cron)
    ├── No request/response
    └── Good for: Background jobs, cleanup

Queue Trigger
    ├── Triggered when message in queue
    ├── Process and delete message
    └── Good for: Async task processing

Blob Trigger
    ├── Triggered when blob created/modified
    ├── Pass blob info to function
    └── Good for: Image processing, file handling
```

#### Bindings (Input/Output)
```
Input Bindings
    ├── Queue Storage (read from queue)
    ├── Blob Storage (read blob)
    ├── SQL Database (query database)
    └── Key Vault (read secret)

Output Bindings
    ├── Queue Storage (write message)
    ├── Blob Storage (create blob)
    ├── SendGrid (send email)
    ├── Cosmos DB (insert document)
    └── Service Bus (publish message)
```

### Pros & Cons

**Pros** ✅
- No infrastructure to manage
- Pay only for execution (cheap)
- Automatic scaling
- Supports many triggers
- Quick to develop
- Great for event-driven

**Cons** ❌
- Cold starts (first execution slow)
- Function timeout (10 minutes typical)
- State is ephemeral (no local state)
- Limited compute power per instance
- Debugging can be challenging
- Vendor lock-in (Azure-specific runtime)

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Execution timeout | 10 minutes (Consumption plan) |
| Memory per function | 1.5GB |
| Concurrent executions | 200 per region (Consumption) |
| Functions per plan | Unlimited |
| Bindings per function | 10 per function |

### Real-World Use Cases

#### Use Case 1: Image Processing Pipeline
```
1. User uploads image to Blob Storage
   └─ Triggers Blob Storage event

2. Azure Function triggered
   ├── Download original image
   ├── Resize for web (800x600)
   ├── Create thumbnail (200x200)
   ├── Upload resized versions
   └── Update database

Cost:
    ├── Storage: ~$0.01/image
    └── Function execution: ~$0.0000002 per request
    (Total: Extremely cheap)
```

#### Use Case 2: Scheduled Email Digest
```
Every morning at 9am (Timer trigger)
    └─ Azure Function
    ├── Query orders from last 24 hours
    ├── Build email digest
    ├── Send via SendGrid
    └── Log completion

Cost: ~$0/month (included in free tier)
```

#### Use Case 3: WebHook Handler
```
GitHub webhook: Code pushed
    ├── Call Azure Function HTTP endpoint
    ├── Function triggers build pipeline
    ├── Function validates commit message
    ├── Function posts result as comment
    └── Return 200 to GitHub

Real-time CI/CD trigger
```

### Performance Tips
- Use Premium plan to avoid cold starts
- Batch operations where possible
- Implement connection pooling
- Cache external data (API responses)
- Monitor execution time
- Use appropriate function size

### Cost Optimization
- Use Consumption plan (pay-per-execution)
- Free tier: 1 million executions/month
- Batch operations (fewer invocations)
- Clean up unused functions
- Monitor execution time

---

## 3. Logic Apps

### What is it?
Visual, low-code workflow automation platform. Integrate systems without coding.

### When to Use
- Business process automation
- System integration
- Schedule workflows
- No-code automation
- Connect 500+ connectors

### Key Features
- **Visual Designer**: Drag-and-drop workflow
- **500+ Connectors**: Pre-built integrations
- **Triggers**: What starts the workflow
- **Actions**: What workflow does
- **Conditions**: If/else branching
- **Loops**: Repeat actions

### Pros & Cons

**Pros** ✅
- No coding required (visual)
- 500+ pre-built connectors
- Business user friendly
- Flexible scheduling
- Built-in error handling
- Good for orchestration

**Cons** ❌
- Expensive for high-volume
- Limited customization
- Complex logic challenging
- Performance overhead
- Debugging difficult

### Real-World Use Cases

#### Use Case 1: Approval Workflow
```
Manager submits expense report
    ↓
Logic App triggered
    ├── Send approval email to CFO
    ├── Wait for response
    ├── If approved: Create accounting entry
    ├── If rejected: Send rejection email
    └── Archive report

No coding required - pure visual
```

#### Use Case 2: Data Sync
```
New customer in Dynamics 365
    ↓
Logic App triggered
    ├── Get customer details
    ├── Create user in Azure AD
    ├── Provision Office 365 mailbox
    ├── Add to billing system
    ├── Send welcome email
    └── Create CRM record

Orchestrates multiple systems
```

---

## 4. Service Bus (Enterprise Messaging)

### What is it?
Enterprise-grade message queue for reliable, distributed messaging between services.

### When to Use (Already covered in Messaging.md)
- Reliable message delivery
- Distributed transactions
- Decoupling services
- Async processing

---

## Architecture Patterns

### Pattern 1: Serverless Integration Pipeline
```
Event Source
    ├── Blob Storage (file uploaded)
    ├── Service Bus (message received)
    └── HTTP request (webhook)

Trigger Azure Function
    ├── Process data
    ├── Validate input
    ├── Transform format

Output to downstream
    ├── Database (insert)
    ├── Queue (for next step)
    ├── Email (notification)
    └── Dashboard (trigger update)

Cost: ~$1/month for thousands of executions
```

### Pattern 2: API Management + Functions
```
Client
    └─ API Management Gateway
    ├── Rate limiting
    ├── Authentication
    ├── Request logging
    └── Routes to Functions
        ├── GET /products → GetProducts function
        ├── POST /orders → CreateOrder function
        ├── DELETE /orders/{id} → DeleteOrder function
        └── Cost: ~$100/month for API gateway

Benefit: Centralized API management, security, monitoring
```

### Pattern 3: Multi-System Integration
```
Sales System
    ├── Sends order event
    └─ Service Bus

Logistics System
    ├── Consumes order event
    ├── Creates shipment
    └─ Publishes "ShipmentCreated" event

Accounting System
    ├── Consumes "ShipmentCreated"
    ├── Creates invoice
    └─ Publishes "InvoiceCreated" event

Email Service
    ├── Consumes "InvoiceCreated"
    ├── Sends invoice email
    └─ Done

Benefit: Loose coupling, easy to add new systems
```

---

## Interview Questions

1. **Design API for public consumption**
   - API Management gateway
   - Versioning (v1, v2 simultaneously)
   - Rate limiting per subscriber
   - Authentication/authorization
   - Developer portal
   - Monetization/billing

2. **Build event-driven serverless pipeline**
   - Blob Storage trigger → Function
   - Function processes, outputs to Service Bus
   - Logic App consumes Service Bus
   - Integrates with CRM/database

3. **Integrate multiple legacy systems**
   - Service Bus for async messaging
   - Functions for data transformation
   - API Management for API exposure
   - Monitoring/alerting for health

4. **Cost optimization for serverless**
   - Use Consumption plan (pay-per-execution)
   - Batch operations
   - Implement caching
   - Schedule expensive operations
   - Monitor actual usage

5. **Handle long-running processes**
   - Functions: 10-minute timeout
   - Use Service Bus for queuing
   - Use Durable Functions for multi-step workflows
   - Store state in Cosmos DB
   - Implement checkpointing for recovery

---

## Cost Optimization Tips

1. **API Management**: ~$100-1000/month (shared cost per API)
2. **Functions**: ~$0/month (free tier: 1M executions)
3. **Logic Apps**: ~$0.25 per action (expensive for high-volume)
4. **Service Bus**: ~$0.05 per 1M messages
5. **Batch operations**: Reduce execution count

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: API management, serverless, integration, workflows
