using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Newtonsoft.Json;

#nullable enable

namespace Azure101.Integration.Examples;

/// <summary>
/// Real-world microservices communication patterns
/// Covers resilience, retries, circuit breakers, and error handling
/// </summary>
public class MicroservicesPatternsExample
{
    /// <summary>
    /// Pattern 1: Service-to-Service Communication with Resilience
    ///
    /// Real-world scenario:
    /// Order Service needs to call User Service to get customer details
    /// User Service might be slow, down, or return errors
    ///
    /// Solution: Implement retry + circuit breaker + timeout
    /// </summary>
    public class ResilientServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;
        private readonly ILogger<ResilientServiceClient> _logger;

        public ResilientServiceClient(HttpClient httpClient, ILogger<ResilientServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _resiliencePolicy = CreateResiliencePolicy();
        }

        /// <summary>
        /// Creates a resilience policy combining retry + circuit breaker
        ///
        /// Retry Strategy:
        /// - Retry on 5xx errors and timeouts
        /// - Exponential backoff: 100ms → 200ms → 400ms
        /// - Max 3 attempts
        ///
        /// Circuit Breaker Strategy:
        /// - If 50% of last 10 requests fail, open circuit
        /// - Stay open for 10 seconds before trying again
        /// - Goal: Stop hammering failing service
        /// </summary>
        private IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy()
        {
            // Retry policy: retry on transient failures
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult<HttpResponseMessage>(r =>
                    // Retry on 5xx and 408 (timeout), 429 (rate limit)
                    (int)r.StatusCode >= 500 || (int)r.StatusCode == 408 || (int)r.StatusCode == 429)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                    {
                        // Exponential backoff: 100ms, 200ms, 400ms
                        var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100);
                        _logger.LogWarning($"Retrying after {delay.TotalMilliseconds}ms (attempt {attempt})");
                        return delay;
                    },
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount}: {outcome.Result?.StatusCode ?? (System.Net.HttpStatusCode)999}");
                    });

            // Circuit breaker policy: stop calling if service is failing
            var circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(10),
                    onBreak: (outcome, timespan) =>
                    {
                        _logger.LogError($"Circuit breaker opened for {timespan.TotalSeconds}s");
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    });

            // Combine policies: retry first, then circuit breaker
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }

        /// <summary>
        /// Call another service with built-in resilience
        ///
        /// Real usage:
        /// var user = await client.GetAsync<User>("/api/users/123");
        /// </summary>
        public async Task<T?> GetAsync<T>(string url) where T : class
        {
            try
            {
                _logger.LogInformation($"Calling {url}");

                var response = await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    return await _httpClient.GetAsync(url);
                });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Request failed: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<T>(content);
                return result;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogError(ex, "Circuit breaker is open - service unavailable");
                return null; // Fallback: return null or cached value
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling service");
                throw;
            }
        }
    }

    /// <summary>
    /// Pattern 2: Idempotent Message Processing
    ///
    /// Real-world scenario:
    /// Service Bus might deliver the same message twice (at-least-once semantics)
    /// Must process idempotently - if processed twice, same result
    ///
    /// Solution: Check if already processed before doing work
    /// </summary>
    public class IdempotentMessageProcessor
    {
        private readonly ILogger<IdempotentMessageProcessor> _logger;

        // In real world, this would be a database
        private static readonly HashSet<string> ProcessedMessages = new();

        public IdempotentMessageProcessor(ILogger<IdempotentMessageProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process a message idempotently
        ///
        /// Key: Use message ID to track if processed
        /// If message seen before, skip processing (return success)
        /// If new message, process and store ID
        /// </summary>
        public async Task<bool> ProcessOrderMessageAsync(OrderMessage message)
        {
            var messageId = message.MessageId;

            _logger.LogInformation($"Processing message {messageId}");

            // Check if already processed
            if (ProcessedMessages.Contains(messageId))
            {
                _logger.LogInformation($"Message {messageId} already processed, skipping");
                return true; // Return success without reprocessing
            }

            try
            {
                // Simulate order processing
                _logger.LogInformation($"Creating order {message.OrderId}");
                await Task.Delay(100); // Simulate work

                // Mark as processed ONLY after successful processing
                ProcessedMessages.Add(messageId);

                _logger.LogInformation($"Message {messageId} processed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process message {messageId}");
                // Don't mark as processed - will retry
                return false;
            }
        }
    }

    /// <summary>
    /// Pattern 3: Saga Pattern for Distributed Transactions
    ///
    /// Real-world scenario:
    /// Order placed → Process payment → Reserve inventory → Create shipment
    /// If any step fails, compensate previous steps
    ///
    /// Example:
    /// 1. Create order (success)
    /// 2. Process payment (success)
    /// 3. Reserve inventory (FAILS)
    /// 4. Compensate: Refund payment, Cancel order
    /// </summary>
    public class OrderSagaOrchestrator
    {
        private readonly ILogger<OrderSagaOrchestrator> _logger;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IInventoryService _inventoryService;

        public OrderSagaOrchestrator(
            ILogger<OrderSagaOrchestrator> logger,
            IOrderService orderService,
            IPaymentService paymentService,
            IInventoryService inventoryService)
        {
            _logger = logger;
            _orderService = orderService;
            _paymentService = paymentService;
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Execute saga with compensating transactions
        ///
        /// Key principle: Each step has compensating step
        /// If any fails, roll back previous steps
        /// </summary>
        public async Task<bool> ExecuteOrderSagaAsync(CreateOrderRequest request)
        {
            var orderId = Guid.NewGuid().ToString();
            var compensations = new List<Func<Task>>();

            try
            {
                // Step 1: Create Order
                _logger.LogInformation($"Step 1: Creating order {orderId}");
                await _orderService.CreateOrderAsync(new Order
                {
                    Id = orderId,
                    CustomerId = request.CustomerId,
                    Amount = request.Amount,
                    Status = "Pending"
                });
                // Compensation: Cancel order
                compensations.Add(async () =>
                {
                    _logger.LogInformation($"Compensating: Cancelling order {orderId}");
                    await _orderService.CancelOrderAsync(orderId);
                });

                // Step 2: Process Payment
                _logger.LogInformation($"Step 2: Processing payment for order {orderId}");
                var paymentId = await _paymentService.ProcessPaymentAsync(new PaymentRequest
                {
                    OrderId = orderId,
                    Amount = request.Amount,
                    CustomerId = request.CustomerId
                });
                // Compensation: Refund payment
                compensations.Add(async () =>
                {
                    _logger.LogInformation($"Compensating: Refunding payment {paymentId}");
                    await _paymentService.RefundAsync(paymentId);
                });

                // Step 3: Reserve Inventory (might fail)
                _logger.LogInformation($"Step 3: Reserving inventory for order {orderId}");
                var reservationId = await _inventoryService.ReserveStockAsync(new InventoryRequest
                {
                    OrderId = orderId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
                // Compensation: Release reservation
                compensations.Add(async () =>
                {
                    _logger.LogInformation($"Compensating: Releasing reservation {reservationId}");
                    await _inventoryService.ReleaseReservationAsync(reservationId);
                });

                _logger.LogInformation($"Order {orderId} saga completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Order {orderId} saga failed, executing compensations");

                // Execute compensations in reverse order
                for (int i = compensations.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await compensations[i]();
                    }
                    catch (Exception compensationEx)
                    {
                        _logger.LogError(compensationEx, "Compensation failed - manual intervention needed");
                    }
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Pattern 4: Distributed Tracing with Correlation ID
    ///
    /// Real-world scenario:
    /// Request flows through: API Gateway → Order Service → User Service → Database
    /// Need to trace across all services for debugging
    ///
    /// Solution: Generate correlation ID, pass through all calls
    /// </summary>
    public class DistributedTracingExample
    {
        private readonly ILogger<DistributedTracingExample> _logger;
        private readonly HttpClient _httpClient;

        public DistributedTracingExample(ILogger<DistributedTracingExample> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Call service with correlation ID
        ///
        /// In production:
        /// - Correlation ID generated at API Gateway
        /// - Passed to all downstream services
        /// - Logged everywhere
        /// - Visible in Application Insights for request tracing
        /// </summary>
        public async Task<string> CallUserServiceAsync(string correlationId, int userId)
        {
            var url = $"https://user-service/api/users/{userId}";

            _logger.LogInformation("Calling User Service with CorrelationId: {CorrelationId}", correlationId);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Correlation-Id", correlationId);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("User Service response received. CorrelationId: {CorrelationId}", correlationId);

            return content;
        }
    }

    /// <summary>
    /// Pattern 5: Handling Cascading Failures
    ///
    /// Real-world scenario:
    /// Order Service calls Payment Service, Payment Service calls Bank API
    /// Bank is slow, Payment Service times out, Order Service backs up
    /// Soon Order Service overwhelmed, system fails
    ///
    /// Solution: Timeout, circuit breaker, bulkhead (connection pooling)
    /// </summary>
    public class CascadingFailureProtection
    {
        private readonly ILogger<CascadingFailureProtection> _logger;
        private readonly HttpClient _httpClient;

        public CascadingFailureProtection(ILogger<CascadingFailureProtection> logger)
        {
            _logger = logger;
            // Configure timeout: don't wait forever
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // 5 second timeout
            };
        }

        /// <summary>
        /// Prevent cascading failures with timeout + circuit breaker
        ///
        /// Without these protections:
        /// - Bank API slow (100 second response)
        /// - Payment Service calls Bank with no timeout
        /// - All connections blocked, Payment Service down
        /// - Order Service piles up requests, also down
        /// - Entire system cascades down
        ///
        /// With protections:
        /// - Timeout at 5 seconds
        /// - Return error to Order Service
        /// - Circuit breaker prevents more calls
        /// - System stays responsive
        /// </summary>
        public async Task<PaymentResult?> ProcessPaymentWithTimeoutAsync(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing payment with 5 second timeout");

                var url = $"https://payment-service/api/payments";
                var content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json");

                // Timeout enforced by HttpClient timeout (5 seconds)
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Payment failed: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<PaymentResult>(responseContent);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Payment request timed out after 5 seconds");
                return null; // Return error to caller, don't retry forever
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing payment");
                throw;
            }
        }
    }

    /// <summary>
    /// Pattern 6: Bulkhead Pattern (Connection Pooling)
    ///
    /// Real-world scenario:
    /// Order Service makes 100 concurrent calls to User Service
    /// User Service connection pool exhausted, all requests fail
    ///
    /// Solution: Limit concurrent requests with semaphore
    /// </summary>
    public class BulkheadPatternExample
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<BulkheadPatternExample> _logger;
        private readonly HttpClient _httpClient;

        public BulkheadPatternExample(ILogger<BulkheadPatternExample> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            // Limit to 10 concurrent requests
            _semaphore = new SemaphoreSlim(10);
        }

        /// <summary>
        /// Process request with bulkhead protection
        ///
        /// Without:
        /// - 100 concurrent requests
        /// - Connections pile up
        /// - Service overwhelmed
        ///
        /// With:
        /// - Max 10 concurrent requests
        /// - Others wait in queue
        /// - Service stays responsive
        /// </summary>
        public async Task<User?> GetUserWithBulkheadAsync(int userId)
        {
            await _semaphore.WaitAsync(); // Acquire permit (max 10)

            try
            {
                _logger.LogInformation($"Getting user {userId}, {_semaphore.CurrentCount} permits available");

                var response = await _httpClient.GetAsync($"https://user-service/api/users/{userId}");
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<User>(content);
            }
            finally
            {
                _semaphore.Release(); // Release permit
            }
        }
    }
}

// ==================== Models ====================

public class OrderMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class PaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string CustomerId { get; set; } = string.Empty;
}

public class PaymentResult
{
    public string PaymentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class InventoryRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ==================== Service Interfaces ====================

public interface IOrderService
{
    Task CreateOrderAsync(Order order);
    Task CancelOrderAsync(string orderId);
}

public interface IPaymentService
{
    Task<string> ProcessPaymentAsync(PaymentRequest request);
    Task RefundAsync(string paymentId);
}

public interface IInventoryService
{
    Task<string> ReserveStockAsync(InventoryRequest request);
    Task ReleaseReservationAsync(string reservationId);
}
