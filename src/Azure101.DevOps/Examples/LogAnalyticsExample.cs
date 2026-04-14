using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Azure101.DevOps.Examples
{
    /// <summary>
    /// Azure Log Analytics - Centralized Logging and Querying
    /// Demonstrates: Log ingestion, KQL queries, custom metrics, analysis
    /// Production pattern: Central repository for all logs and metrics
    /// Benefits: Find issues fast, audit trail, compliance reporting
    /// </summary>
    public class LogAnalyticsExample
    {
        private readonly LogsQueryClient _logsClient;
        private readonly ILogger<LogAnalyticsExample> _logger;
        private readonly string _workspaceId;

        public LogAnalyticsExample(
            string workspaceId,
            ILogger<LogAnalyticsExample> logger)
        {
            _logger = logger;
            _workspaceId = workspaceId;
            var credential = new DefaultAzureCredential();
            _logsClient = new LogsQueryClient(credential);
        }

        // ============================================================================
        // Pattern 1: Query Recent Logs
        // ============================================================================
        /// <summary>
        /// Retrieve logs from last N hours
        /// Use case: Troubleshooting recent issue
        /// Performance: Returns results in < 1 second
        /// </summary>
        public async Task<List<LogEntry>> QueryRecentLogsAsync(
            string logTable,
            int lastHours = 1,
            int limitResults = 100)
        {
            try
            {
                _logger.LogInformation($"→ Querying {logTable} from last {lastHours} hours");

                // Build Kusto Query Language (KQL) query
                string query = $@"
                    {logTable}
                    | where TimeGenerated > ago({lastHours}h)
                    | order by TimeGenerated desc
                    | limit {limitResults}";

                // Execute query
                var response = await _logsClient.QueryWorkspaceAsync(
                    new Guid(_workspaceId),
                    query,
                    new QueryTimeRange(TimeSpan.FromHours(lastHours)));

                var logs = new List<LogEntry>();

                // Parse results
                foreach (var table in response.Value.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        logs.Add(new LogEntry
                        {
                            Timestamp = DateTime.Parse(row[0].ToString()),
                            Level = row[1]?.ToString() ?? "Unknown",
                            Message = row[2]?.ToString() ?? "",
                            Service = row[3]?.ToString() ?? ""
                        });
                    }
                }

                _logger.LogInformation($"✓ Retrieved {logs.Count} log entries");
                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Query failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Find Errors and Exceptions
        // ============================================================================
        /// <summary>
        /// Search for specific error patterns
        /// Use case: Find all 'OutOfMemory' errors in past 24 hours
        /// </summary>
        public async Task<List<ErrorSummary>> SearchErrorsAsync(
            string errorPattern,
            int hoursBack = 24)
        {
            try
            {
                _logger.LogInformation($"→ Searching for errors matching: {errorPattern}");

                string query = $@"
                    traces
                    | where TimeGenerated > ago({hoursBack}h)
                    | where message contains ""{errorPattern}""
                    | summarize Count = count(), FirstOccurrence = min(TimeGenerated)
                      by message, tostring(customDimensions.Service)
                    | order by Count desc";

                // Execute query (in real scenario)
                var errors = new List<ErrorSummary>
                {
                    new ErrorSummary
                    {
                        ErrorMessage = "Database connection timeout",
                        Count = 47,
                        FirstOccurrence = DateTime.UtcNow.AddHours(-2),
                        Service = "OrderService"
                    },
                    new ErrorSummary
                    {
                        ErrorMessage = "OutOfMemoryException",
                        Count = 12,
                        FirstOccurrence = DateTime.UtcNow.AddHours(-1),
                        Service = "ReportService"
                    }
                };

                _logger.LogInformation($"✓ Found {errors.Count} error patterns");
                foreach (var error in errors)
                {
                    _logger.LogInformation($"  {error.ErrorMessage}: {error.Count} times");
                }

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Error search failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Performance Analysis (Percentiles)
        // ============================================================================
        /// <summary>
        /// Analyze performance distribution
        /// P50 (median), P95, P99 response times
        /// Use case: Understand performance: "Normal=100ms, bad=1000ms"
        /// </summary>
        public async Task<PerformanceDistribution> AnalyzePerformanceAsync(
            string service)
        {
            try
            {
                _logger.LogInformation($"→ Analyzing performance for {service}");

                string query = $@"
                    customMetrics
                    | where cloud_RoleInstance == ""{service}""
                    | where TimeGenerated > ago(7d)
                    | summarize
                        P50 = percentile(value, 50),
                        P95 = percentile(value, 95),
                        P99 = percentile(value, 99),
                        Avg = avg(value),
                        Max = max(value)
                      by name";

                var distribution = new PerformanceDistribution
                {
                    Service = service,
                    P50ResponseTimeMs = 95,
                    P95ResponseTimeMs = 450,
                    P99ResponseTimeMs = 2100,
                    AverageResponseTimeMs = 200,
                    MaxResponseTimeMs = 15000
                };

                _logger.LogInformation($"✓ Performance analysis:");
                _logger.LogInformation($"  P50 (median): {distribution.P50ResponseTimeMs}ms");
                _logger.LogInformation($"  P95 (slow): {distribution.P95ResponseTimeMs}ms");
                _logger.LogInformation($"  P99 (slowest): {distribution.P99ResponseTimeMs}ms");

                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Performance analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Trend Analysis (Over Time)
        // ============================================================================
        /// <summary>
        /// Track metrics over time (hourly/daily trends)
        /// Use case: "Error rate increasing? Capacity growing?"
        /// </summary>
        public async Task<List<TrendDataPoint>> AnalyzeTrendAsync(
            string metricName,
            int daysBack = 7)
        {
            try
            {
                _logger.LogInformation($"→ Analyzing trend for {metricName}");

                string query = $@"
                    customMetrics
                    | where name == ""{metricName}""
                    | where TimeGenerated > ago({daysBack}d)
                    | summarize Value = avg(value) by bin(TimeGenerated, 1h)
                    | order by TimeGenerated asc";

                var trend = new List<TrendDataPoint>
                {
                    new TrendDataPoint { Time = DateTime.UtcNow.AddHours(-24), Value = 100 },
                    new TrendDataPoint { Time = DateTime.UtcNow.AddHours(-12), Value = 115 },
                    new TrendDataPoint { Time = DateTime.UtcNow.AddHours(-6), Value = 142 },
                    new TrendDataPoint { Time = DateTime.UtcNow.AddHours(-3), Value = 180 },
                    new TrendDataPoint { Time = DateTime.UtcNow, Value = 220 }
                };

                _logger.LogInformation($"✓ Trend analysis: {metricName}");
                _logger.LogInformation($"  Current: {trend[trend.Count-1].Value}");
                _logger.LogInformation($"  24h ago: {trend[0].Value}");
                _logger.LogInformation($"  Growth: {((trend[trend.Count-1].Value / trend[0].Value - 1) * 100):F0}%");

                return trend;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Trend analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Correlation Analysis (Root Cause)
        // ============================================================================
        /// <summary>
        /// Find correlation between events
        /// Use case: "When errors spike, what else changes?"
        /// Example: Errors spike when DB CPU > 80%
        /// </summary>
        public async Task<CorrelationAnalysis> FindCorrelationAsync()
        {
            try
            {
                _logger.LogInformation("→ Analyzing correlations");

                string query = @"
                    let errors = customEvents
                        | where name == 'ErrorOccurred'
                        | project TimeGenerated, ErrorCount = 1;

                    let dbCpu = customMetrics
                        | where name == 'DatabaseCPU'
                        | project TimeGenerated, CPU = value;

                    errors
                    | join kind=inner dbCpu on $left.TimeGenerated == $right.TimeGenerated
                    | summarize ErrorCount = sum(ErrorCount), AvgCPU = avg(CPU)
                      by bin(TimeGenerated, 5m)
                    | where AvgCPU > 80";

                var correlation = new CorrelationAnalysis
                {
                    Event1 = "Application Errors",
                    Event2 = "Database CPU Usage",
                    CorrelationCoefficient = 0.87f,
                    Insight = "Errors spike when Database CPU > 80%"
                };

                _logger.LogInformation($"✓ Correlation found:");
                _logger.LogInformation($"  {correlation.Event1} ↔ {correlation.Event2}");
                _logger.LogInformation($"  Coefficient: {correlation.CorrelationCoefficient:F2}");
                _logger.LogInformation($"  Insight: {correlation.Insight}");

                return correlation;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Correlation analysis failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Aggregation (Group By)
        // ============================================================================
        /// <summary>
        /// Summarize data by dimensions
        /// Use case: "Which service has most errors?"
        /// </summary>
        public async Task<List<ServiceMetrics>> AggregateByServiceAsync()
        {
            try
            {
                _logger.LogInformation("→ Aggregating metrics by service");

                string query = @"
                    exceptions
                    | where TimeGenerated > ago(24h)
                    | summarize
                        ErrorCount = count(),
                        UniqueUsers = dcount(user_Id),
                        AvgDuration = avg(duration),
                        LastError = max(TimeGenerated)
                      by cloud_RoleName
                    | order by ErrorCount desc
                    | limit 10";

                var metrics = new List<ServiceMetrics>
                {
                    new ServiceMetrics
                    {
                        ServiceName = "PaymentService",
                        ErrorCount = 234,
                        AffectedUsers = 45,
                        AvgDurationMs = 1200
                    },
                    new ServiceMetrics
                    {
                        ServiceName = "OrderService",
                        ErrorCount = 87,
                        AffectedUsers = 23,
                        AvgDurationMs = 500
                    },
                    new ServiceMetrics
                    {
                        ServiceName = "InventoryService",
                        ErrorCount = 12,
                        AffectedUsers = 3,
                        AvgDurationMs = 800
                    }
                };

                _logger.LogInformation($"✓ Service metrics:");
                foreach (var service in metrics)
                {
                    _logger.LogInformation($"  {service.ServiceName}: {service.ErrorCount} errors, {service.AffectedUsers} users");
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Aggregation failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 7: Log Analytics Best Practices
        // ============================================================================
        public static class BestPractices
        {
            public const string LOGGING_STRATEGY = @"
                WHAT TO LOG:

                ✓ Log these (important for troubleshooting):
                  - Exceptions and stack traces
                  - Business events (user login, order created)
                  - External API calls (success/failure)
                  - Important state changes
                  - Warnings and errors

                ✗ Don't log (expensive and useless):
                  - Debug logs from every function
                  - Full request/response bodies
                  - Timestamps on every line (already included)
                  - Personal data (email, passwords, credit cards)

                PERFORMANCE TIPS:
                - Use structured logging (JSON) not free text
                - Filter at source (don't send all logs)
                - Set appropriate severity levels (Info, Warning, Error)
                - Batch log writes
                - Use sampling for high-volume events";

            public const string QUERY_OPTIMIZATION = @"
                Fast Queries:
                ✓ Query last 1-7 days (recommended)
                ✓ Filter early (where before summarize)
                ✓ Use exact matches (==) not contains
                ✓ Limit results (| limit 100)

                Slow Queries:
                ✗ Query last 90+ days without good filter
                ✗ Scan entire table for single value
                ✗ Complex string matching (regex)
                ✗ Return millions of rows

                EXAMPLE - BAD (scans all data):
                | where message contains 'error'  // Slow

                EXAMPLE - GOOD (filtered):
                | where TimeGenerated > ago(24h)
                | where severity == 'Error'  // Fast";

            public const string RETENTION_POLICY = @"
                Default: 30 days (standard)
                Extended: 90 days (additional cost)
                Long-term: 365+ days (data export)

                Cost: $2.50/GB per month

                Cost Optimization:
                - Keep only critical logs long-term
                - Archive old logs to blob storage ($0.01/GB/month)
                - Compliance requirement? Archive instead";
        }

        // ============================================================================
        // Pattern 8: Alerts from Log Analytics
        // ============================================================================
        public static class LogAlerts
        {
            public const string ALERT_RULES = @"
                ALERT TYPE 1: Error Rate Spike
                Query: 'count by severity'
                Condition: 'Error count > 100 in 5 minutes'
                Action: Send email + Teams notification

                ALERT TYPE 2: Performance Degradation
                Query: 'avg(duration) by bin(5m)'
                Condition: 'Average duration > 2000ms'
                Action: Page on-call engineer

                ALERT TYPE 3: Resource Exhaustion
                Query: 'CustomMetrics with CPU usage'
                Condition: 'CPU > 85% for 10 minutes'
                Action: Auto-scale, send alert

                ALERT TYPE 4: Business Metric
                Query: 'count by customDimensions.OrderStatus'
                Condition: 'Orders/hour < 50 (unusual drop)'
                Action: Notify product team

                Alert Best Practice:
                - Set threshold based on baseline
                - Use multi-condition to reduce noise
                - Link to runbook (what to do)
                - Test alert in staging first";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Service { get; set; }
    }

    public class ErrorSummary
    {
        public string ErrorMessage { get; set; }
        public int Count { get; set; }
        public DateTime FirstOccurrence { get; set; }
        public string Service { get; set; }
    }

    public class PerformanceDistribution
    {
        public string Service { get; set; }
        public double P50ResponseTimeMs { get; set; }
        public double P95ResponseTimeMs { get; set; }
        public double P99ResponseTimeMs { get; set; }
        public double AverageResponseTimeMs { get; set; }
        public double MaxResponseTimeMs { get; set; }
    }

    public class TrendDataPoint
    {
        public DateTime Time { get; set; }
        public double Value { get; set; }
    }

    public class CorrelationAnalysis
    {
        public string Event1 { get; set; }
        public string Event2 { get; set; }
        public float CorrelationCoefficient { get; set; }
        public string Insight { get; set; }
    }

    public class ServiceMetrics
    {
        public string ServiceName { get; set; }
        public int ErrorCount { get; set; }
        public int AffectedUsers { get; set; }
        public double AvgDurationMs { get; set; }
    }
}
