# Azure Messaging Services - Complete Wiki

## Overview

Azure Messaging provides asynchronous, loosely-coupled communication between services. Choose based on messaging pattern (queue, pub/sub, streaming) and latency/ordering requirements.

---

## Quick Comparison Table

| Service | Pattern | Throughput | Latency | Retention | Best For |
|---------|---------|-----------|---------|-----------|----------|
| **Service Bus Queue** | Queue | 2,000 msg/sec | 1-10ms | Up to 1 day | Reliable queuing, at-most-once |
| **Service Bus Topic** | Pub/Sub | 2,000 msg/sec | 1-10ms | Up to 1 day | Multi-subscriber, filtering |
| **Event Hubs** | Streaming | 1+ million evt/sec | < 1ms | 24+ hours | Big data, time-series |
| **Event Grid** | Event broker | 500,000 evt/sec | < 100ms | Transient | Reactive, event-driven |
| **Queue Storage** | Queue | Unlimited | 1-10ms | Up to 7 days | Simple, cheap queuing |

---

## 1. Azure Service Bus Queue

### What is it?
Reliable, enterprise-grade message queue with FIFO ordering, dead-lettering, and exactly-once processing.

### When to Use
- Reliable message delivery required
- Distributed transactions (saga pattern)
- Decoupling sender from receiver
- Load leveling (spiky traffic)
- Guaranteed message processing

### Key Features
- **FIFO Ordering**: Messages processed in order
- **Dead-Letter Queue**: Failed messages moved here
- **Session**: Group related messages
- **Auto-Forward**: Automatically move to another queue
- **Exactly-Once**: No duplicates guaranteed
- **Scheduled Messages**: Send at specific time

### Architecture & Core Concepts

#### Queue vs Topic
```
Queue (1-to-1)
├── Sender → Message → Queue → Receiver
├── One receiver processes each message
└── Good for task distribution

Topic (1-to-Many)
├── Sender → Message → Topic
├── Topic → Subscription 1 → Receiver 1
├── Topic → Subscription 2 → Receiver 2
├── Multiple receivers see same message
└── Good for event distribution
```

#### Message Processing Flow
```
1. Sender publishes message
2. Message stored in queue (durable)
3. Receiver fetches message
4. Receiver processes
5. Receiver acknowledges (completes)
6. Message deleted from queue

If step 4 fails:
    └─ Message returned to queue
    └─ Retry configured times
    └─ Move to dead-letter queue if exhausted
```

### Pros & Cons

**Pros** ✅
- Guaranteed delivery
- FIFO ordering
- Dead-letter queue for failed messages
- Session support for ordering groups
- Exactly-once processing (no duplicates)
- High throughput (2000 msg/sec)

**Cons** ❌
- Higher latency than Event Hubs
- More expensive than Queue Storage
- Maximum message size 1MB
- No complex filtering
- Less suitable for high-volume streaming

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Queue size | 5GB (standard) to 80GB (premium) |
| Message size | 1MB |
| Throughput | 2000 msg/sec (standard), 4000+ (premium) |
| Message retention | Up to 1 day (queue), 14 days (dead-letter) |
| Sessions per queue | Unlimited |
| Concurrent connections | 5000 |

### Real-World Use Cases

#### Use Case 1: Order Processing
```
Order Service
    └─ Publish: "OrderCreated" message
    └─ Service Bus Queue

Payment Service
    ├─ Consume: "OrderCreated"
    ├─ Process payment
    └─ Publish: "PaymentProcessed" or "PaymentFailed"

Inventory Service
    ├─ Consume: "PaymentProcessed"
    ├─ Deduct inventory
    └─ Publish: "InventoryDeducted"

Shipping Service
    ├─ Consume: "InventoryDeducted"
    ├─ Create shipment
    └─ Publish: "ShippingCreated"

Result: Decoupled services, reliable pipeline
```

#### Use Case 2: Email Queue
```
Application
    └─ Queue: "SendEmail" messages

Email Service
    ├─ Process queue
    ├─ Attempt to send
    ├─ If fails, retry 3 times
    └─ Move to DLQ if exhausted
    (Email admin investigates dead-letter queue)
```

### Performance Tips
- Use PrefetchCount to batch messages
- Batch acknowledgments
- Use sessions for message grouping
- Filter at client level, not server
- Monitor dead-letter queue regularly
- Implement idempotent processing

### Cost Optimization
- Queue Storage for simple queuing (cheaper)
- Service Bus for reliable, critical paths
- Archive processed messages to blob storage
- Monitor queue depth and adjust receivers
- Clean up old messages automatically

---

## 2. Azure Service Bus Topic

### What is it?
Publish/subscribe messaging pattern. One sender publishes to topic, multiple subscribers receive messages.

### When to Use
- Event distribution to multiple subscribers
- Broadcasting information
- Fan-out scenarios
- Topic-based routing
- Decoupling publishers from subscribers

### Key Features
- **Topics**: Receive published messages
- **Subscriptions**: Filter and receive from topic
- **Filters**: Advanced message routing
- **Auto-Delete**: Remove unused subscriptions
- **Rule Filters**: Route messages by properties

### Pros & Cons

**Pros** ✅
- Multiple subscribers to one message
- Advanced message filtering
- Auto-forward to other topics
- SQL and correlation ID filters
- Works with dead-letter queues

**Cons** ❌
- More complex than queues
- Subscribers must exist before publish (or miss message)
- Not good for back-pressure (slow subscribers)
- Cost per subscription

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Topic size | 5GB (standard) to 80GB (premium) |
| Subscriptions per topic | 2000 |
| Message size | 1MB |
| Rules per subscription | 2000 |

### Real-World Use Cases

#### Use Case 1: Domain Event Publishing
```
UserService publishes to "UserEvents" topic
├── User Created
├── User Updated
└── User Deleted

Subscriptions:
├── EmailService: Send welcome/confirmation emails
├── AnalyticsService: Track user analytics
├── NotificationService: Alert interested parties
└── SyncService: Sync to data warehouse

Result: One event, multiple consumers
```

#### Use Case 2: Stock Price Distribution
```
MarketData Service
    └─ Publish: "StockPriceChanged" to topic

Subscriptions:
├── TradingService: Auto-trade based on rules
├── AnalyticsService: Store for analysis
├── DashboardService: Update UI in real-time
└── AlertService: Notify clients of unusual moves
```

---

## 3. Azure Event Hubs

### What is it?
Big data streaming platform for high-volume event ingestion (millions of events/second) with temporal buffering and replay.

### When to Use
- IoT data ingestion (sensors, devices)
- Application telemetry
- User activity streams
- High-volume event streaming
- Data replay capability needed

### Key Features
- **Massive Throughput**: 1+ million events/second
- **Capture**: Auto-archive events to blob storage
- **Consumer Groups**: Multiple independent consumers
- **Partitioning**: Distribute across partitions
- **Retention**: 24+ hours (up to 90 days)
- **Replay**: Reprocess historical events

### Architecture & Core Concepts

#### Throughput Units (TUs)
```
1 TU = 1MB/sec ingress, 2MB/sec egress
Scaling:
- 1 TU: Small IoT deployment
- 4 TU: Medium traffic (400k events/sec)
- 20 TU: Large scale (2M events/sec)
- Auto-scale: Dynamically adjust based on load
```

#### Partitioning Strategy
```
Partition 0 ← Events with key=A
Partition 1 ← Events with key=B
Partition 2 ← Events with key=C
Partition 3 ← Events with key=D

Benefits:
├── Parallelization (4 partitions, 4 consumers)
├── Order within partition guaranteed
└── Throughput scales with partitions

Tradeoff: More partitions = more complexity
```

### Pros & Cons

**Pros** ✅
- Extreme throughput (1M+ events/sec)
- Temporal buffering (replay capability)
- Multiple consumer groups
- Automatic scaling
- Excellent for time-series data
- Built-in Kafka compatibility

**Cons** ❌
- More complex than Service Bus
- Ordering only within partition
- Not ideal for one-off events
- Requires understanding of partitioning
- Higher cost than simpler options

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Throughput units | Auto-scale up to 40 TU (can increase) |
| Partitions | 32 (can increase) |
| Event size | 1MB (compressed) |
| Consumer groups | Unlimited |
| Retention | 24 hours (standard), up to 90 (premium) |
| Ingestion rate | 1MB/sec per TU |

### Real-World Use Cases

#### Use Case 1: IoT Sensor Data
```
1 Million Sensors
├── Each sends temperature every 10 seconds
├── = 100k events/sec to Event Hubs
├── Partitioned by sensor-id (1000 partitions)
└── Distributed stream processing

Results:
├── Real-time dashboard: Query last 24h
├── Analytics pipeline: Historical analysis
└── Alerts: Anomaly detection
```

#### Use Case 2: Application Telemetry
```
Web App (1000 instances)
└─ Each sends events to Event Hubs
├── Page views: 100k/sec
├── Clicks: 500k/sec
├── Errors: 1k/sec
└── Total: 600k/sec

Processing:
├── Stream Analytics: Real-time aggregations
├── Databricks: Batch analytics
└── Power BI: Dashboards

Cost: ~$2000/month for 20 TU
```

#### Use Case 3: Activity Stream with Replay
```
Ecommerce Platform
├── Capture all user activity to Event Hubs
├── User clicks: 10 million/hour
├── Purchases: 100k/hour
├── Views: 1 million/hour

Replay capabilities:
├── Replay last 7 days for new analytics
├── Re-process for bug fixes
└── Recompute metrics from historical data
```

### Performance Tips
- Partition by consistent key (user-id, device-id)
- Use batch sending (10+ events per request)
- Implement consumer group per consumer type
- Monitor lag between producers and consumers
- Implement exponential backoff on errors
- Archive to blob storage for cold data

### Cost Optimization
- Use auto-scale to match actual demand
- Archive old events (90+ days) to blob storage
- Batch sends to reduce requests
- Use consumer groups efficiently
- Monitor partition count (more partitions = higher cost)

---

## 4. Azure Event Grid

### What is it?
Serverless event broker that routes events from sources to handlers with millisecond latency and massive scale.

### When to Use
- React to events (blob created, VM deleted, etc)
- Decouple event producers and consumers
- Serverless event routing
- Azure resource events
- Custom application events

### Key Features
- **Sources**: 100+ Azure services + custom
- **Topics**: Custom event routing
- **Handlers**: Functions, Logic Apps, Webhooks, Queues
- **Filtering**: Advanced event filtering
- **Delivery**: Guaranteed at-least-once delivery
- **Deadletter**: Failed events stored

### Pros & Cons

**Pros** ✅
- Serverless (no infrastructure to manage)
- Sub-100ms latency
- 500k+ events/sec
- Integration with 100+ Azure services
- Built-in retry and deadletter
- Webhook support for custom integrations

**Cons** ❌
- No ordering guarantees
- Not suitable for heavy processing
- Limited filtering compared to Service Bus
- Transient delivery model
- Not ideal for guaranteed delivery needs

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Event Grid topics per region | 1000 |
| Handlers per topic | 100 |
| Event size | 1MB |
| Throughput | 500k events/sec |
| Retry attempts | 30 (24 hours) |

### Real-World Use Cases

#### Use Case 1: Image Processing Pipeline
```
User uploads image to Blob Storage
    └─ Event Grid detects "BlobCreated"
    └─ Triggers Azure Function
    ├── Function downloads blob
    ├── Resizes image
    ├── Creates thumbnail
    └── Uploads to CDN

Entire pipeline: Automatic, no polling
```

#### Use Case 2: Resource Monitoring
```
Azure Resources
├── VM deleted
├── SQL Database scaled
├── Storage quota exceeded
    └─ All trigger Event Grid events

Event handlers:
├── Log to Event Hub (audit trail)
├── Send notification via SendGrid
└── Trigger Logic App (approval workflow)
```

#### Use Case 3: Microservices Sync
```
OrderService publishes:
├── OrderCreated
├── OrderCancelled
├── OrderShipped

Event Grid routes to:
├── InventoryService
├── ShippingService
├── AnalyticsService
├── NotificationService

Result: Loose coupling, automatic scaling
```

---

## 5. Azure Queue Storage

### What is it?
Simple, durable message queue backed by blob storage. Extremely cost-effective for basic queuing.

### When to Use
- Simple task queuing
- Cost-sensitive scenarios
- High-volume, low-complexity messaging
- Asynchronous processing
- No strict ordering needed

### Key Features
- **FIFO by default**: Messages in order (best-effort)
- **Visibility timeout**: Message invisible while processing
- **TTL**: Auto-delete old messages
- **Scalable**: Unlimited messages
- **Cheap**: Lowest cost option

### Pros & Cons

**Pros** ✅
- Extremely cheap (~$0.40/million messages)
- Unlimited scale
- Simple to use
- Automatic scaling
- Works with all Azure tooling

**Cons** ❌
- Best-effort FIFO (not guaranteed)
- Limited to 7 days retention
- 64KB message size (without workarounds)
- No built-in filtering
- Weaker SLA than Service Bus

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Queue size | Unlimited |
| Message size | 64KB (65KB with encoding) |
| Retention | Up to 7 days |
| Messages per second | Unlimited |

### Real-World Use Cases

#### Use Case 1: Bulk Data Processing
```
Batch Job
├── Generate 1 million tasks
├── Put in Queue Storage
└── Cost: ~$0.40 for all

Workers
├── Pull from queue
├── Process
├── Acknowledge
└── Auto-scale based on queue depth
```

#### Use Case 2: Email Queueing
```
Web App
├── User requests email
├── Put in Queue Storage (instant)
└── Return to user

Background Worker
├── Process queue
├── Send via SMTP
└── Retry on failure
```

---

## Architecture Patterns

### Pattern 1: Fan-Out with Service Bus Topics
```
OrderService → OrderCreated event
    └─ Service Bus Topic: "OrderEvents"
    ├─ EmailService (subscription)
    ├─ AnalyticsService (subscription)
    ├─ InventoryService (subscription)
    └─ ShippingService (subscription)

Result: One event, many processors
```

### Pattern 2: High-Volume IoT Ingestion
```
Millions of IoT Devices
    └─ Event Hubs (1M events/sec)
    ├─ Stream Analytics (real-time)
    ├─ Azure Databricks (analytics)
    └─ Archive to Blob Storage (cold storage)
```

### Pattern 3: Retry + Dead Letter Queue
```
Message arrives in Service Bus
    ├─ Try to process
    ├─ If fails, retry 3 times
    └─ After 3 failures, move to DLQ
    
Admin can later:
    ├─ Review DLQ messages
    ├─ Fix root cause
    └─ Resubmit to main queue
```

### Pattern 4: Event-Driven Processing
```
Azure Storage Event
    ├─ Blob created
    └─ Event Grid → Azure Function
        ├── Resize image
        ├── Generate thumbnail
        └── Update database
```

---

## Comparison & Selection Guide

### When to Use Service Bus Queue
- Guarantee every message processed exactly once
- Ordering critical
- Dead-letter queue important
- Distributed transactions (saga)

### When to Use Service Bus Topic
- Multiple subscribers to same event
- Fan-out scenarios
- Topic-based routing

### When to Use Event Hubs
- High-volume streaming (1M+ events/sec)
- Time-series data
- Replay capability
- IoT scenarios

### When to Use Event Grid
- React to Azure resource events
- Serverless event routing
- Sub-100ms latency
- Custom webhook integration

### When to Use Queue Storage
- Simple, cheap queuing
- Cost is primary concern
- No strict delivery guarantees needed
- High volume, simple messages

---

## Interview Questions

1. **Design order processing pipeline with distributed saga**
   - Order Service publishes to Service Bus Queue
   - Payment Service consumes, publishes to Queue
   - Inventory Service consumes, publishes to Queue
   - Compensating transactions in DLQ for failures

2. **Ingest 1M IoT events per second**
   - Use Event Hubs (not Service Bus)
   - Partition by device-id
   - Stream Analytics for real-time aggregation
   - Archive to blob storage for history

3. **Ensure message processing exactly once**
   - Idempotent processing (check if already processed)
   - Service Bus with exactly-once guarantee
   - Dead-letter queue for failed messages
   - Audit trail for compliance

4. **Monitor queue depth and auto-scale**
   - Azure Monitor alerts on queue depth
   - Auto-scale rules based on metric
   - Trigger more worker instances
   - Cost optimization: scale down when empty

5. **Choose between Service Bus vs Queue Storage**
   - Service Bus: Reliability, ordering, DLQ critical
   - Queue Storage: Cost is main concern, simple messages

6. **Event-driven architecture design**
   - Event Grid for Azure resource events
   - Service Bus Topics for application events
   - Webhooks for external integrations
   - Compensating transactions for rollback

---

## Cost Optimization Tips

1. **Queue Storage is cheapest** (~$0.40/M messages)
2. **Service Bus for critical paths** (exactly-once, ordering)
3. **Event Hubs for high volume** (streaming data)
4. **Archive old events** to blob storage
5. **Monitor actual throughput** and right-size
6. **Batch operations** to reduce requests
7. **Event Grid is serverless** (pay-per-event only)

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Asynchronous communication, event-driven architecture
