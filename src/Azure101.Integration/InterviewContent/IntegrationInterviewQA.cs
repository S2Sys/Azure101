using System;
using System.Collections.Generic;

namespace Azure101.Integration.InterviewContent
{
    public static class IntegrationInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            ("Design API for third-party developers with versioning and rate limiting",
            "1. Use API Management. 2. API versions: /api/v1 (old), /api/v2 (new, recommended). 3. Rate limit: 1000 req/min per subscription tier. 4. Authentication: API key or OAuth. 5. Developer portal: Self-service, docs, sandbox. 6. Monitoring: Track usage, quota, errors. 7. Deprecation: 6-month warning before shutting down v1.",
            "API Design"),

            ("Implement serverless order processing with Functions and Service Bus",
            "1. Order created → Timer function triggers every 1 minute. 2. Query pending orders from database. 3. For each: Publish to Service Bus topic. 4. Multiple functions consume: PaymentFunction, InventoryFunction, ShippingFunction. 5. Each function: Process, update database, publish next event. 6. Cost: Pay per execution (cheap, ~$0.20/million invocations).",
            "Serverless Architecture"),

            ("Connect on-premises data to Azure using Logic Apps",
            "1. On-Prem: Install Data Gateway (acts as bridge). 2. Logic App: Create workflow. 3. Trigger: On HTTP request. 4. Action: Query on-prem SQL database via gateway. 5. Action: Transform data (format). 6. Action: Write to Azure SQL. 7. Response: Return status to client. 8. Cost: ~$10/month per gateway.",
            "Hybrid Integration"),

            ("Design webhook integration with external SaaS (Salesforce, HubSpot)",
            "1. External SaaS: When event happens (lead created), send HTTP POST to webhook. 2. Azure: Logic App or Function receives webhook. 3. Parse: Extract data (contact info, company). 4. Process: Validate, enrich. 5. Store: Write to SQL database or Dynamics. 6. Notify: Send email confirmation. 7. Retry: 3 attempts if webhook fails.",
            "Webhook Integration"),

            ("Implement pub/sub pattern with Service Bus Topics for notifications",
            "1. Order Service: Publishes ""OrderCreated"" event to topic. 2. Multiple subscribers (email, SMS, push): Each gets copy. 3. Email Subscriber: Listens, sends confirmation email. 4. SMS Subscriber: Listens, sends SMS. 5. Analytics Subscriber: Listens, updates dashboard. 6. Benefits: New subscriber added without changing publisher.",
            "Pub/Sub Pattern"),

            ("How do you handle Azure Function timeouts and long-running tasks?",
            "Problem: Function has 10-minute timeout, but task takes 30 minutes. Solution: 1. Use Durable Functions (orchestration). 2. Chain multiple functions: Process 1 → Process 2 → Process 3. 3. Each completes within 10 minutes. 4. Durable Functions manage state, resumption. 5. Total: Can run days if needed.",
            "Long-Running Tasks")
        };
    }
}
