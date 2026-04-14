using System;
using System.Collections.Generic;

namespace Azure101.Messaging.InterviewContent
{
    /// <summary>
    /// Interview Q&A for Azure Messaging Services
    /// Service Bus, Event Hubs, Event Grid, Queue Storage
    /// </summary>
    public static class MessagingInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            (
                "Design a distributed order processing system using Service Bus",
                @"
Scenario: E-commerce with multiple services (Order, Payment, Inventory, Shipping)

Architecture:

Order Service (Producer)
├── User places order
├── Publish ""OrderCreated"" message to Service Bus Queue
└── Immediately return to user (async)

    ↓ Service Bus Queue: ""orders-created""

Payment Service (Consumer 1)
├── Consume: OrderCreated
├── Process payment
├── Publish: PaymentProcessed (success) OR PaymentFailed (error)
└── Delete message from queue

    ↓ Service Bus Queue: ""payments-processed""

Inventory Service (Consumer 2)
├── Consume: PaymentProcessed
├── Deduct inventory
├── Publish: InventoryDeducted OR OutOfStock
└── Delete message from queue

    ↓ Service Bus Queue: ""inventory-deducted""

Shipping Service (Consumer 3)
├── Consume: InventoryDeducted
├── Create shipment
├── Publish: ShippingCreated
└── Delete message from queue

    ↓

Customer Notification Service (Consumer 4)
├── Consume: ShippingCreated
├── Send confirmation email
└── Delete message from queue

Benefits:
✓ Loose coupling (each service independent)
✓ Async processing (fast user experience)
✓ Reliable delivery (messages queued if service down)
✓ Easy to add new services (just add consumer)

Failure Handling:

Scenario: Payment Service fails
1. Message stays in queue
2. Automatic retry (3 times)
3. If still fails: Move to Dead-Letter Queue (DLQ)
4. Manual investigation: Admin reviews DLQ
5. Fix issue, resubmit message

Code Example:
// Order Service: Publish
await serviceBusClient.CreateSender(""orders-created"")
    .SendMessageAsync(new ServiceBusMessage(orderData));

// Payment Service: Consume
var receiver = serviceBusClient.CreateReceiver(""orders-created"");
var message = await receiver.ReceiveMessageAsync();
try {
    ProcessPayment(message);
    await receiver.CompleteMessageAsync(message);  // Success
} catch (Exception ex) {
    // Will retry automatically
    throw;
}
                ",
                "Distributed Architecture"
            ),

            (
                "Design a real-time analytics pipeline for 1M events/second",
                @"
Scenario: Mobile app sending events (clicks, views, purchases)

Architecture:

1M Events/Second
├── Mobile apps worldwide
└── Send to Event Hubs (sub-millisecond ingestion)

    ↓ Event Hubs (streaming service)
    ├── 20 throughput units (1M events/sec)
    ├── Partition count: 32 (parallelization)
    └── Retention: 24 hours (replay capability)

    ↓ Two parallel paths:

REAL-TIME PATH (Streaming):
├── Stream Analytics
├── Window: Tumbling 1-minute
├── Queries:
│   ├── Count events by event type
│   ├── Sum revenue by product
│   └── Count unique users
└── Output: SQL Database (1-minute aggregates)

    ↓ Power BI (Real-time Dashboard)
    ├── Refresh every minute
    ├── Show: Event counts, revenue, user metrics
    └── Alert: If any metric anomalous

BATCH PATH (Analytics):
├── Event Hubs Capture (auto-archive)
├── Store to Data Lake (ADLS)
├── Run Spark job (nightly)
├── Analyze patterns:
│   ├── User cohorts
│   ├── Product affinity
│   └── Churn prediction
└── Output: Data Warehouse (historical data)

    ↓ Reporting (Dashboards, ML models)

Real-Time Example Query:
SELECT
    EventType,
    COUNT(*) as event_count,
    SUM(Revenue) as total_revenue,
    AVG(Duration) as avg_duration
FROM events
GROUP BY EventType, TumblingWindow(minute, 1)

Output (every minute):
EventType           | event_count | total_revenue | avg_duration
Click               | 600,000     | 0             | 0.2s
Purchase            | 10,000      | 500,000       | 5.0s
View                | 390,000     | 0             | 2.0s

Scaling:
├── 1M events/sec = 86B events/day
├── Event Hubs: Auto-scale to 40+ TUs if needed
├── Stream Analytics: Auto-scale to 100+ units
└── Cost: ~$5000-10000/month

Batch Analytics (Nightly):
Data in ADLS (86B events):
├── Partition by date/hour
├── Spark cluster processes in parallel
├── Results: Patterns, trends, anomalies
└── Feed to ML models (churn prediction, etc)

Benefits:
✓ Real-time dashboards (1-minute latency)
✓ Batch analytics (historical analysis)
✓ Automatic scaling (handles peaks)
✓ Fault-tolerant (Event Hubs retains 24h)
✓ Cost-optimized (separate real-time + batch)
                ",
                "Real-Time Analytics"
            ),

            (
                "Handle poison messages and dead-letter queues",
                @"
Problem: Some messages fail repeatedly

Example:
Order message: {orderId: 123, customerId: invalid_id}
├── Service tries to process
├── Error: CustomerId not found
├── Retry 1: Fails again
├── Retry 2: Fails again
├── Retry 3: Fails again
└── Move to Dead-Letter Queue

Dead-Letter Queue Handling:

Step 1: Monitor DLQ
├── Alert when messages arrive in DLQ
├── Use Application Insights:

var metricsClient = new MetricsClient();
metricsClient.GetMetric(\"deadletter_queue_count\");
// Alert if > 0

Step 2: Analyze DLQ Message
var dlqReceiver = client.CreateReceiver(\"orders-created\", \"$DeadLetterQueue\");
var msg = await dlqReceiver.ReceiveMessageAsync();

var deadLetterReason = msg.DeadLetterReason;
// Example: \"IbmDeadLetterReason: InvalidMessageFormat\"

var deadLetterErrorDescription = msg.DeadLetterErrorDescription;
// Example: \"CustomerId field missing or invalid\"

Step 3: Categorize Problem

Category A: Data Issue (90% of cases)
├── Invalid data in message
├── Cannot be fixed by retry
├── Requires manual correction
├── Example: Missing required field, invalid format

Category B: Service Issue (10%)
├── Service temporarily unavailable
├── Fixing and resubmitting helps
├── Example: Database connection timeout

Step 4: Fix and Resubmit

For Data Issues:
1. Fix data in database
2. Create new message with corrected data
3. Resend to queue

For Service Issues:
1. Fix underlying service
2. Resubmit original message
3. Should process successfully

Code for Resubmission:
var dlqMessage = /* get from DLQ */;
var originalBody = dlqMessage.Body.ToString();

// Fix the data
var fixedData = RepairData(originalBody);

// Create new message
var newMessage = new ServiceBusMessage(fixedData);

// Send back to main queue
var sender = client.CreateSender(\"orders-created\");
await sender.SendMessageAsync(newMessage);

Step 5: Automation

Implement intelligent DLQ handler:

if (deadLetterReason == \"InvalidData\") {
    // Category A: Requires investigation
    SendAlert(\"DLQ message requires manual fix\");
    SendToManualQueueForReview();
} else if (deadLetterReason == \"ServiceUnavailable\") {
    // Category B: Can retry
    var retryMessage = new ServiceBusMessage(originalBody);
    var delay = TimeSpan.FromMinutes(5);
    await sender.ScheduleMessageAsync(retryMessage, DateTime.Now + delay);
}

Monitoring DLQ Health:

Key Metrics:
├── Messages in DLQ (should be 0)
├── DLQ arrival rate (should be < 1/minute)
├── DLQ age (how long messages sit)
├── Fix rate (how many fixed vs ignored)

Alerting:
├── If DLQ count > 10: Critical alert
├── If DLQ arrival > 5/minute: Warning
└── If messages in DLQ > 24 hours: Investigate

Expected Numbers:
Good: 0 messages in DLQ (perfect)
Acceptable: < 1 message per 1000 (0.1% error rate)
Bad: > 10 messages in DLQ (investigate immediately)
Critical: > 100 messages in DLQ (system problem)
                ",
                "Error Handling"
            ),

            (
                "Compare Service Bus Queue vs Event Hubs vs Event Grid",
                @"
Three Messaging Patterns:

1. SERVICE BUS QUEUE (Reliable Queuing)
   Use: Task distribution, processing pipeline
   Throughput: 2,000 msg/sec
   Delivery: Exactly-once (no duplicates)
   Ordering: FIFO (same sequence)
   Retention: Up to 1 day
   Cost: ~$0.05 per 1M messages

   Example: Order processing
   Order → Queue → Multiple consumers → Process

2. EVENT HUBS (Streaming)
   Use: Telemetry, big data, high-volume
   Throughput: 1M+ events/sec
   Delivery: At-least-once (may have duplicates)
   Ordering: Per partition (not global)
   Retention: 24+ hours (replay)
   Cost: ~$0.01-0.03 per 1M events

   Example: IoT sensor data
   Sensors → Event Hub → Stream Analytics → Dashboard

3. EVENT GRID (Event Routing)
   Use: React to events, serverless
   Throughput: 500k events/sec
   Delivery: Best-effort
   Ordering: No guarantee
   Retention: Transient only
   Cost: ~$0.50 per 1M events

   Example: Blob created → Trigger function
   Upload file → Auto-process → Email result

Quick Decision:
├── Need guaranteed delivery? → Service Bus
├── High-volume streaming? → Event Hubs
├── React to Azure events? → Event Grid
└── Complex processing pipeline? → Service Bus

Detailed Comparison Table:

Feature              | Service Bus | Event Hubs   | Event Grid
Throughput          | 2K/sec      | 1M+/sec      | 500K/sec
Delivery guarantee  | Exactly-once| At-least-once| Best-effort
Ordering            | FIFO        | Per partition| No order
Max message size    | 1MB         | 1MB          | 1MB
Retention time      | 1 day       | 24h-90d      | Transient
Cost per 1M         | $0.05       | $0.01-0.03   | $0.50
Setup complexity    | Medium      | Medium       | Low
Support languages   | All         | All          | All
Dead-letter queue   | Yes         | Yes          | No

Real-World Scenario Selection:

Scenario 1: Email Queue
├── Consistency: Must send every email (no loss)
├── Volume: 1000 emails/day
├── Processing: Sequential, one-by-one
└── Choice: Service Bus Queue ✓

Scenario 2: Website Analytics
├── Consistency: Missing 1% OK
├── Volume: 100k events/sec
├── Processing: Aggregate, not individual
└── Choice: Event Hubs ✓

Scenario 3: Blob Uploaded
├── Consistency: Try best, don't retry
├── Volume: 100 blobs/day
├── Processing: Trigger function immediately
└── Choice: Event Grid ✓
                ",
                "Service Selection"
            )
        };
    }
}
