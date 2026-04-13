using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

#nullable enable

namespace Azure101.Messaging.Examples;

/// <summary>
/// Production patterns for Azure Service Bus and API Gateway
/// Real-world scenarios from microservices architecture
/// </summary>
public class ServiceBusAndAPIGatewayExample
{
    /// <summary>
    /// Pattern 1: Service Bus Message Producer
    ///
    /// Real-world scenario:
    /// Order Service places order and publishes event to Service Bus
    /// Multiple services (Notification, Inventory, Shipping) subscribe to event
    ///
    /// Benefits:
    /// - Decoupled: Order Service doesn't know about subscribers
    /// - Asynchronous: Doesn't wait for subscribers
    /// - Scalable: Subscribers process at own pace
    /// - Resilient: Message persisted if subscriber fails
    /// </summary>
    public class OrderEventProducer
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<OrderEventProducer> _logger;

        public OrderEventProducer(ServiceBusSender sender, ILogger<OrderEventProducer> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        /// <summary>
        /// Publish order event to Service Bus topic
        ///
        /// In production, this would be:
        /// - Called when order is created in database
        /// - Message includes order ID, customer, amount
        /// - Multiple subscribers process independently
        /// </summary>
        public async Task PublishOrderCreatedEventAsync(OrderCreatedEvent @event)
        {
            try
            {
                var messageBody = JsonConvert.SerializeObject(@event);
                var message = new ServiceBusMessage(messageBody)
                {
                    CorrelationId = @event.CorrelationId,
                    MessageId = @event.EventId,
                    ContentType = "application/json",
                    Subject = "OrderCreated", // For filtering on subscriber side
                    TimeToLive = TimeSpan.FromDays(7) // Message expires after 7 days
                };

                // Add custom properties (useful for subscription filters)
                message.ApplicationProperties.Add("CustomerTier", @event.CustomerTier);
                message.ApplicationProperties.Add("OrderAmount", @event.Amount);

                _logger.LogInformation(
                    "Publishing OrderCreated event. OrderId: {OrderId}, CorrelationId: {CorrelationId}",
                    @event.OrderId,
                    @event.CorrelationId);

                await _sender.SendMessageAsync(message);

                _logger.LogInformation("OrderCreated event published successfully. OrderId: {OrderId}", @event.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event. OrderId: {OrderId}", @event.OrderId);
                throw;
            }
        }

        /// <summary>
        /// Batch publish multiple events
        ///
        /// Production use case:
        /// - Import 1000 orders from legacy system
        /// - Publish all as events
        /// - Subscribers process asynchronously
        /// </summary>
        public async Task PublishOrderBatchAsync(IEnumerable<OrderCreatedEvent> events)
        {
            var batch = await _sender.CreateMessageBatchAsync();

            foreach (var @event in events)
            {
                var messageBody = JsonConvert.SerializeObject(@event);
                var message = new ServiceBusMessage(messageBody)
                {
                    MessageId = @event.EventId,
                    CorrelationId = @event.CorrelationId,
                    ContentType = "application/json"
                };

                // If message doesn't fit in batch, send batch and create new one
                if (!batch.TryAddMessage(message))
                {
                    await _sender.SendMessagesAsync(batch);
                    batch = await _sender.CreateMessageBatchAsync();
                    batch.TryAddMessage(message);
                }
            }

            // Send remaining messages
            if (batch.Count > 0)
            {
                await _sender.SendMessagesAsync(batch);
                _logger.LogInformation("Batch of {Count} events published", batch.Count);
            }
        }
    }

    /// <summary>
    /// Pattern 2: Service Bus Message Consumer
    ///
    /// Real-world scenario:
    /// Notification Service subscribes to OrderCreated events
    /// Processes each order to send customer notification
    /// Must handle failures gracefully (retry, dead-letter queue)
    /// </summary>
    public class OrderEventConsumer
    {
        private readonly ServiceBusProcessor _processor;
        private readonly ILogger<OrderEventConsumer> _logger;
        private readonly INotificationService _notificationService;

        public OrderEventConsumer(
            ServiceBusProcessor processor,
            ILogger<OrderEventConsumer> logger,
            INotificationService notificationService)
        {
            _processor = processor;
            _logger = logger;
            _notificationService = notificationService;

            // Register handlers
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += HandleErrorAsync;
        }

        /// <summary>
        /// Handle incoming message from Service Bus
        ///
        /// Key pattern: Idempotent processing
        /// - If processed twice, same result
        /// - Check if already processed
        /// - Only complete message on success
        /// </summary>
        private async Task HandleMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                var messageBody = args.Message.Body.ToString();
                var @event = JsonConvert.DeserializeObject<OrderCreatedEvent>(messageBody)
                    ?? throw new InvalidOperationException("Failed to deserialize message");

                var correlationId = args.Message.CorrelationId;

                _logger.LogInformation(
                    "Processing OrderCreated event. OrderId: {OrderId}, CorrelationId: {CorrelationId}",
                    @event.OrderId,
                    correlationId);

                // Check if already processed (idempotency)
                if (await IsAlreadyProcessedAsync(@event.EventId))
                {
                    _logger.LogInformation(
                        "Event already processed. EventId: {EventId}, skipping",
                        @event.EventId);
                    // Complete message without reprocessing
                    await args.CompleteMessageAsync(args.CancellationToken);
                    return;
                }

                // Send notification
                await _notificationService.SendOrderConfirmationAsync(
                    @event.CustomerId,
                    @event.OrderId,
                    @event.Amount,
                    correlationId);

                // Mark as processed in idempotency store
                await MarkAsProcessedAsync(@event.EventId);

                // Only complete message after successful processing
                await args.CompleteMessageAsync(args.CancellationToken);

                _logger.LogInformation("Event processed successfully. EventId: {EventId}", @event.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                // Don't complete message - it will be retried
                // After max retries, goes to dead-letter queue
                await args.AbandonMessageAsync(args.CancellationToken);
            }
        }

        /// <summary>
        /// Handle errors from Service Bus
        /// Called when message processing throws exception
        /// </summary>
        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(
                args.Exception,
                "Service Bus error. Entity: {Entity}, Action: {Action}",
                args.EntityPath,
                args.Action);

            // Log for monitoring/alerting
            // In production, this triggers alert to on-call engineer
            return Task.CompletedTask;
        }

        /// <summary>
        /// Check if event already processed (idempotency)
        ///
        /// In production:
        /// - Query database: SELECT * FROM ProcessedEvents WHERE EventId = ?
        /// - If exists, return true
        /// - Otherwise, return false
        /// </summary>
        private async Task<bool> IsAlreadyProcessedAsync(string eventId)
        {
            // Simulate database query
            await Task.Delay(10);
            return false; // In real app, query database
        }

        /// <summary>
        /// Mark event as processed (idempotency)
        ///
        /// In production:
        /// - INSERT INTO ProcessedEvents (EventId, ProcessedAt) VALUES (?, NOW())
        /// - If already exists, update timestamp
        /// </summary>
        private async Task MarkAsProcessedAsync(string eventId)
        {
            // Simulate database insert
            await Task.Delay(10);
        }

        public async Task StartAsync()
        {
            await _processor.StartProcessingAsync();
            _logger.LogInformation("Order event consumer started");
        }

        public async Task StopAsync()
        {
            await _processor.StopProcessingAsync();
            _logger.LogInformation("Order event consumer stopped");
        }
    }

    /// <summary>
    /// Pattern 3: Dead-Letter Queue Handling
    ///
    /// Real-world scenario:
    /// Message fails after max retries (e.g., malformed JSON)
    /// Goes to dead-letter queue for manual review
    /// Operations team investigates and either:
    /// 1. Fixes the message and replays
    /// 2. Deletes if unrecoverable
    /// </summary>
    public class DeadLetterQueueHandler
    {
        private readonly ServiceBusReceiver _deadLetterReceiver;
        private readonly ILogger<DeadLetterQueueHandler> _logger;

        public DeadLetterQueueHandler(ServiceBusReceiver deadLetterReceiver, ILogger<DeadLetterQueueHandler> logger)
        {
            _deadLetterReceiver = deadLetterReceiver;
            _logger = logger;
        }

        /// <summary>
        /// Monitor dead-letter queue for failed messages
        ///
        /// In production:
        /// - Scheduled job runs every 5 minutes
        /// - Checks for new DLQ messages
        /// - Logs alert for operations team
        /// - Operations investigates and fixes
        /// </summary>
        public async Task ProcessDeadLetterQueueAsync()
        {
            try
            {
                var messages = await _deadLetterReceiver.ReceiveMessagesAsync(maxMessages: 10);

                if (!messages.Any())
                {
                    _logger.LogDebug("No messages in dead-letter queue");
                    return;
                }

                _logger.LogWarning("Found {Count} messages in dead-letter queue", messages.Count);

                foreach (var message in messages)
                {
                    var deadLetterReason = message.DeadLetterReason;
                    var deadLetterErrorDescription = message.DeadLetterErrorDescription;

                    _logger.LogError(
                        "Dead-letter message. MessageId: {MessageId}, Reason: {Reason}, Description: {Description}",
                        message.MessageId,
                        deadLetterReason,
                        deadLetterErrorDescription);

                    // Log additional context for operations team
                    _logger.LogError("Message body: {Body}", message.Body.ToString());

                    // Determine if message is fixable
                    var isFixable = IsPoisonMessage(message) == false;

                    if (isFixable)
                    {
                        // Could automatically fix or alert operations
                        _logger.LogWarning("Message might be fixable, alerting operations team");
                    }
                    else
                    {
                        // Poison message (malformed), delete it
                        await _deadLetterReceiver.CompleteMessageAsync(message);
                        _logger.LogWarning("Poison message deleted. MessageId: {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dead-letter queue");
            }
        }

        private bool IsPoisonMessage(ServiceBusReceivedMessage message)
        {
            // Check if message is malformed (poison)
            try
            {
                JsonConvert.DeserializeObject(message.Body.ToString());
                return false; // Valid JSON, not poison
            }
            catch
            {
                return true; // Invalid JSON, poison message
            }
        }
    }

    /// <summary>
    /// Pattern 4: API Gateway - Request Routing
    ///
    /// Real-world scenario:
    /// Single entry point for all client requests
    /// Routes to appropriate microservice based on URL
    /// Adds authentication headers for downstream calls
    /// </summary>
    public class APIGatewayRouter
    {
        private readonly ILogger<APIGatewayRouter> _logger;
        private readonly HttpClient _httpClient;

        public APIGatewayRouter(ILogger<APIGatewayRouter> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Route request to appropriate service
        ///
        /// API Gateway pattern:
        /// - /api/orders/* → Order Service
        /// - /api/users/* → User Service
        /// - /api/products/* → Product Service
        /// </summary>
        public async Task<string> RouteRequestAsync(
            string path,
            string method,
            string body,
            string authToken,
            string correlationId)
        {
            var targetService = DetermineTargetService(path);

            _logger.LogInformation(
                "Routing {Method} {Path} to {Service}. CorrelationId: {CorrelationId}",
                method,
                path,
                targetService,
                correlationId);

            // Build request to downstream service
            var request = new HttpRequestMessage
            {
                Method = new HttpMethod(method),
                RequestUri = new Uri($"https://{targetService}/api{path}")
            };

            // Add authentication header (from gateway to service)
            request.Headers.Add("Authorization", $"Bearer {authToken}");

            // Add correlation ID for tracing
            request.Headers.Add("X-Correlation-Id", correlationId);

            // Add body if present
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);

                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Received response from {Service}. Status: {StatusCode}",
                    targetService,
                    response.StatusCode);

                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error routing request to {Service}", targetService);
                throw;
            }
        }

        /// <summary>
        /// Determine which service to route to based on path
        /// </summary>
        private string DetermineTargetService(string path)
        {
            if (path.StartsWith("/orders"))
                return "order-service";
            if (path.StartsWith("/users"))
                return "user-service";
            if (path.StartsWith("/products"))
                return "product-service";

            throw new InvalidOperationException($"Unknown path: {path}");
        }
    }

    /// <summary>
    /// Pattern 5: API Gateway - Rate Limiting
    ///
    /// Real-world scenario:
    /// Prevent single client from overwhelming the system
    /// Limit: 1000 requests per minute per client
    /// </summary>
    public class RateLimiter
    {
        private readonly Dictionary<string, RateLimitBucket> _buckets = new();
        private readonly ILogger<RateLimiter> _logger;

        public RateLimiter(ILogger<RateLimiter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Check if request should be allowed
        ///
        /// Production implementation:
        /// - Use distributed cache (Redis)
        /// - Track per IP address or API key
        /// - Increment counter per request
        /// - Reset every minute
        /// </summary>
        public bool IsAllowedAsync(string clientId, int maxRequestsPerMinute = 1000)
        {
            var now = DateTime.UtcNow;

            // Get or create bucket for this client
            if (!_buckets.TryGetValue(clientId, out var bucket))
            {
                bucket = new RateLimitBucket { ResetTime = now.AddMinutes(1) };
                _buckets[clientId] = bucket;
            }

            // Reset bucket if time window expired
            if (now > bucket.ResetTime)
            {
                bucket.RequestCount = 0;
                bucket.ResetTime = now.AddMinutes(1);
            }

            // Check if over limit
            if (bucket.RequestCount >= maxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                return false;
            }

            // Increment counter
            bucket.RequestCount++;
            return true;
        }

        private class RateLimitBucket
        {
            public int RequestCount { get; set; }
            public DateTime ResetTime { get; set; }
        }
    }

    /// <summary>
    /// Pattern 6: API Gateway - Authentication
    ///
    /// Real-world scenario:
    /// Validate JWT token at gateway
    /// Extract user info, pass to services via header
    /// </summary>
    public class AuthenticationHandler
    {
        private readonly ILogger<AuthenticationHandler> _logger;

        public AuthenticationHandler(ILogger<AuthenticationHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate and extract user info from JWT token
        ///
        /// In production:
        /// - Validate JWT signature
        /// - Check expiration
        /// - Extract claims (user ID, roles)
        /// - Pass to services via X-User-Id header
        /// </summary>
        public bool ValidateAndExtractUser(string? token, out string? userId)
        {
            userId = null;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Missing authentication token");
                return false;
            }

            try
            {
                // In production: Validate JWT signature and expiration
                // This is simplified example
                var parts = token.Split('.');
                if (parts.Length != 3)
                {
                    _logger.LogWarning("Invalid token format");
                    return false;
                }

                // Extract payload (simplified - in production use JWT library)
                var payloadJson = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(parts[1]));

                var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(payloadJson)
                    ?? new Dictionary<string, object>();

                userId = payload.TryGetValue("sub", out var sub) ? sub?.ToString() : null;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Missing user ID in token");
                    return false;
                }

                _logger.LogInformation("Token validated for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }
    }
}

// ==================== Models ====================

public class OrderCreatedEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerTier { get; set; } = "Standard";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

public interface INotificationService
{
    Task SendOrderConfirmationAsync(string customerId, string orderId, decimal amount, string correlationId);
}
