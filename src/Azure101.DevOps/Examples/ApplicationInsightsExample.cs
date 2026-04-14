using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;

namespace Azure101.DevOps.Examples
{
    /// <summary>
    /// Azure Application Insights - Application Performance Monitoring (APM)
    /// Demonstrates: Logging, metrics, dependencies, exceptions, custom events
    /// Production pattern: Real-time visibility into application health
    /// Benefits: Detect issues before users report them, understand performance
    /// </summary>
    public class ApplicationInsightsExample
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ILogger<ApplicationInsightsExample> _logger;

        public ApplicationInsightsExample(
            TelemetryClient telemetryClient,
            ILogger<ApplicationInsightsExample> logger)
        {
            _telemetryClient = telemetryClient;
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Logging Events
        // ============================================================================
        /// <summary>
        /// Record events (user actions, business logic)
        /// Use case: Understand user behavior, track feature usage
        /// Example: User logged in, order created, payment processed
        /// Cost: $2.30 per GB ingested
        /// </summary>
        public async Task<bool> LogEventAsync(string eventName, Dictionary<string, string> properties)
        {
            try
            {
                _logger.LogInformation($"→ Logging event: {eventName}");

                // Log custom event
                _telemetryClient.TrackEvent(
                    name: eventName,
                    properties: properties);

                _logger.LogInformation($"✓ Event logged: {eventName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Event logging failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Track Exceptions (Error Monitoring)
        // ============================================================================
        /// <summary>
        /// Automatically capture and log exceptions
        /// Benefits: Identify failing code patterns
        /// Example: "Database timeout errors increasing 50%"
        /// Alert: If error rate > 5%, notify team
        /// </summary>
        public async Task<object> ProcessOrderAsync(int orderId)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation($"→ Processing order {orderId}");

                // Simulate database operation
                if (orderId < 0)
                    throw new ArgumentException("Invalid order ID");

                await Task.Delay(100); // Simulate work

                _logger.LogInformation($"✓ Order {orderId} processed");
                return new { orderId, status = "success" };
            }
            catch (Exception ex)
            {
                // Track exception automatically
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "OrderId", orderId.ToString() },
                    { "ProcessingTime", (DateTime.UtcNow - startTime).TotalMilliseconds.ToString() }
                });

                _logger.LogError($"✗ Error processing order: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Track Metrics (Quantitative Measurements)
        // ============================================================================
        /// <summary>
        /// Track numerical metrics (measurements)
        /// Use case: Performance monitoring, business metrics
        /// Examples: Response time, orders/minute, inventory count
        /// </summary>
        public async Task<PerformanceMetrics> GetAndTrackMetricsAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("→ Executing operation with metrics tracking");

                // Simulate work
                await Task.Delay(150);

                sw.Stop();

                var metrics = new PerformanceMetrics
                {
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    RequestCount = 1,
                    ErrorCount = 0,
                    CacheHitRate = 0.87f
                };

                // Track metrics
                _telemetryClient.GetMetric("ResponseTime").TrackValue(metrics.ResponseTimeMs);
                _telemetryClient.GetMetric("RequestsPerSecond").TrackValue(1);
                _telemetryClient.GetMetric("CacheHitRate").TrackValue(metrics.CacheHitRate * 100);

                _logger.LogInformation($"✓ Metrics tracked:");
                _logger.LogInformation($"  Response time: {metrics.ResponseTimeMs}ms");
                _logger.LogInformation($"  Cache hit rate: {metrics.CacheHitRate:P}");

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Metrics tracking failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Dependency Tracking (Database, API, Cache)
        // ============================================================================
        /// <summary>
        /// Track external dependencies (database, APIs, caches)
        /// Benefits: Identify slow external services
        /// Example: "Database queries taking 2 seconds"
        /// Alert: If dependency > 1 second, notify team
        /// </summary>
        public async Task<List<OrderData>> GetOrdersWithDependencyTrackingAsync(int customerId)
        {
            var startTime = DateTime.UtcNow;
            const string dependencyType = "SQL";
            const string dependencyName = "OrdersDatabase.GetOrders";
            bool success = false;

            try
            {
                _logger.LogInformation($"→ Querying database for customer {customerId}");

                // Simulate database query
                await Task.Delay(250);

                var orders = new List<OrderData>
                {
                    new OrderData { OrderId = 1, Amount = 100m },
                    new OrderData { OrderId = 2, Amount = 200m }
                };

                success = true;

                _logger.LogInformation($"✓ Database query returned {orders.Count} orders");
                return orders;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Database query failed: {ex.Message}");
                throw;
            }
            finally
            {
                // Track dependency call
                var duration = DateTime.UtcNow - startTime;
                _telemetryClient.TrackDependency(
                    dependencyType,
                    dependencyName,
                    commandName: $"SELECT * FROM Orders WHERE CustomerId = {customerId}",
                    startTime: startTime,
                    duration: duration,
                    success: success);

                _logger.LogInformation($"  Query duration: {duration.TotalMilliseconds:F0}ms");
            }
        }

        // ============================================================================
        // Pattern 5: Request Tracking (HTTP Performance)
        // ============================================================================
        /// <summary>
        /// Track HTTP request performance
        /// Automatic tracking via middleware
        /// Metrics: Response time, status code, user info
        /// </summary>
        public async Task<RequestPerformance> TrackHttpRequestAsync(string endpoint, string method)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation($"→ {method} {endpoint}");

                // Simulate HTTP request processing
                await Task.Delay(50);

                var duration = DateTime.UtcNow - startTime;

                // Track request
                var requestTelemetry = new RequestTelemetry
                {
                    Name = $"{method} {endpoint}",
                    StartTime = startTime,
                    Duration = duration,
                    ResponseCode = "200",
                    Success = true
                };

                _telemetryClient.TrackRequest(requestTelemetry);

                _logger.LogInformation($"✓ {method} {endpoint} - 200 OK ({duration.TotalMilliseconds:F0}ms)");

                return new RequestPerformance
                {
                    Endpoint = endpoint,
                    Method = method,
                    DurationMs = duration.TotalMilliseconds,
                    StatusCode = 200,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Request failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Custom Properties (Context)
        // ============================================================================
        /// <summary>
        /// Add custom context to all telemetry
        /// Examples: User ID, session ID, environment, version
        /// Benefits: Filter and group telemetry by context
        /// </summary>
        public void SetUserContext(string userId, string sessionId)
        {
            try
            {
                _logger.LogInformation($"→ Setting telemetry context for user {userId}");

                // Set global context (applies to all telemetry)
                _telemetryClient.Context.User.Id = userId;
                _telemetryClient.Context.Session.Id = sessionId;
                _telemetryClient.Context.Cloud.RoleInstance = Environment.MachineName;

                // Set custom properties
                _telemetryClient.Context.GlobalProperties["Environment"] = "Production";
                _telemetryClient.Context.GlobalProperties["Version"] = "1.2.3";

                _logger.LogInformation($"✓ Telemetry context configured");
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Context setup failed: {ex.Message}");
            }
        }

        // ============================================================================
        // Pattern 7: Alerts and Notifications
        // ============================================================================
        /// <summary>
        /// Automated alerts based on metric thresholds
        /// </summary>
        public static class AlertConfiguration
        {
            public const string ALERT_EXAMPLES = @"
                ALERT 1: HIGH ERROR RATE
                Condition: Error rate > 5% for 5 minutes
                Action: Send email + Teams message
                Example: 'Production errors spiked to 7% - check logs'

                ALERT 2: SLOW RESPONSES
                Condition: P95 response time > 2 seconds for 10 minutes
                Action: Page on-call engineer
                Example: 'API response time degraded - investigate database'

                ALERT 3: DEPENDENCY FAILURE
                Condition: Database connection fails
                Action: Auto-scale DB, page DBA
                Example: 'Database timeout - scaling up'

                ALERT 4: BUSINESS METRIC
                Condition: Orders/minute < 100 (unusual drop)
                Action: Notify product team
                Example: 'Purchase rate dropped 50% - potential issue'

                ALERT 5: RESOURCE EXHAUSTION
                Condition: Memory usage > 90%
                Action: Restart service, page on-call
                Example: 'Memory leak detected - restarting'";

            public const string ALERT_BEST_PRACTICES = @"
                ✓ THRESHOLD TUNING
                  Too low: False alarms, alert fatigue
                  Too high: Miss real issues
                  Solution: Baseline + baseline * 1.5

                ✓ MULTI-CONDITION ALERTS
                  Don't alert on single metric
                  Alert on: High errors AND high latency
                  Reduces false positives

                ✓ ESCALATION
                  Level 1: Email
                  Level 2: Slack message (after 5 min)
                  Level 3: Page on-call (after 10 min)

                ✓ ALERT FATIGUE
                  Ignore flaky alerts
                  Fix: Adjust threshold or improve reliability

                ✓ RUNBOOK
                  Alert should link to runbook
                  Runbook: 'If you see this alert, do X'";
        }

        // ============================================================================
        // Pattern 8: Querying Telemetry Data
        // ============================================================================
        /// <summary>
        /// Query collected telemetry using Kusto Query Language (KQL)
        /// </summary>
        public static class KustoQueries
        {
            public const string EXAMPLE_QUERIES = @"
                QUERY 1: ERRORS IN LAST HOUR
                customEvents
                | where timestamp > ago(1h)
                | where name == 'OrderFailed'
                | summarize Count = count() by tostring(customDimensions.ErrorCode)

                QUERY 2: P95 RESPONSE TIME BY ENDPOINT
                requests
                | where timestamp > ago(24h)
                | summarize P95 = percentile(duration, 95) by name
                | sort by P95 desc

                QUERY 3: ERROR RATE BY SERVICE
                dependencies
                | where timestamp > ago(1h)
                | summarize ErrorRate = (100.0 * sum(iff(success == false, 1, 0)) / count())
                  by target

                QUERY 4: DATABASE QUERY PERFORMANCE
                dependencies
                | where type == 'SQL'
                | where timestamp > ago(24h)
                | summarize Count = count(), AvgDuration = avg(duration)
                  by name
                | sort by AvgDuration desc

                QUERY 5: USER BEHAVIOR (Page Views)
                pageViews
                | where timestamp > ago(7d)
                | summarize Users = dcount(user_Id), Pageviews = count()
                  by tostring(customDimensions.Page)

                QUERY 6: FIND ROOT CAUSE
                let slow_requests = requests
                  | where timestamp > ago(1h)
                  | where duration > 2000;  // > 2 seconds

                dependencies
                | where timestamp > ago(1h)
                | where target_roleName == 'OrderService'
                | join kind=inner slow_requests on operation_Id";

            public const string BENEFITS = @"
                ✓ TROUBLESHOOTING
                  'Orders service slow?'
                  → Query: P95 response time by service
                  → Find: Database dependency is slow

                ✓ CAPACITY PLANNING
                  'Need more servers?'
                  → Query: Requests/minute trend
                  → Find: Growing 20% monthly

                ✓ RELEASE ANALYSIS
                  'Did deployment help?'
                  → Query: Error rate before/after
                  → Find: 30% improvement

                ✓ PERFORMANCE OPTIMIZATION
                  'Which queries are slow?'
                  → Query: Database query durations
                  → Find: One query takes 95% of time";
        }

        // ============================================================================
        // Pattern 9: Application Insights vs Log Analytics
        // ============================================================================
        public static class InsightsVsLogAnalytics
        {
            public const string COMPARISON = @"
                APPLICATION INSIGHTS:
                Purpose: Application Performance Monitoring
                Data: Exceptions, metrics, dependencies, requests
                Use: Understand app health, performance
                Query: Lightweight, auto-indexed
                Cost: $2.30/GB

                LOG ANALYTICS:
                Purpose: Centralized logging and analysis
                Data: Application logs, system logs, events
                Use: Troubleshooting, audit, compliance
                Query: Powerful KQL, any data type
                Cost: $2.50/GB

                CONNECTED:
                - App Insights sends data to Log Analytics
                - Same Kusto Query Language (KQL)
                - Shared workspace
                - Can query both together";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class PerformanceMetrics
    {
        public long ResponseTimeMs { get; set; }
        public int RequestCount { get; set; }
        public int ErrorCount { get; set; }
        public float CacheHitRate { get; set; }
    }

    public class OrderData
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
    }

    public class RequestPerformance
    {
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public double DurationMs { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
    }
}
