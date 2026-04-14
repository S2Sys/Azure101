using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Azure101.DataAnalytics.Examples
{
    /// <summary>
    /// Azure Stream Analytics - Real-Time Data Processing
    /// Demonstrates: Streaming queries, windowing, aggregation, alerting
    /// Production pattern: Process unbounded data streams in real-time
    /// Use case: Anomaly detection, real-time dashboards, IoT analysis
    /// Latency: Seconds (vs minutes for batch)
    /// </summary>
    public class StreamAnalyticsExample
    {
        private readonly ILogger<StreamAnalyticsExample> _logger;

        public StreamAnalyticsExample(ILogger<StreamAnalyticsExample> logger)
        {
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Streaming Data Architecture
        // ============================================================================
        /// <summary>
        /// Real-time pipeline: IoT Device → Event Hub → Stream Analytics → Output
        /// </summary>
        public class StreamingArchitecture
        {
            public const string PIPELINE = @"
                IoT DEVICES / APPLICATIONS
                │
                ├─ Send events: Sensor readings, user clicks, transactions
                │
                EVENT HUB (Message Broker)
                │
                ├─ Buffer: Holds 1-7 days of events
                │
                STREAM ANALYTICS (Real-time processor)
                │
                ├─ Parse events
                ├─ Filter (only anomalies)
                ├─ Aggregate (sum, avg, max)
                ├─ Join streams
                ├─ Detect patterns
                │
                OUTPUT OPTIONS
                ├─ SQL Database (store alerts)
                ├─ Power BI (real-time dashboard)
                ├─ Service Bus (trigger action)
                ├─ Blob Storage (archive)
                └─ Cosmos DB (NoSQL storage)

                LATENCY: Event → Output = 1-5 seconds
                (vs batch analytics = hours/days)";

            public const string TIMELINE = @"
                Example: IoT temperature monitoring

                2:00:00 PM - Device sends temp=42°C
                2:00:01 PM - Event Hub receives
                2:00:02 PM - Stream Analytics processes
                2:00:02 PM - Query detects: temp > 40° = ALERT
                2:00:02 PM - Output: Alert sent to SQL Database
                2:00:03 PM - Dashboard shows alert (seconds old)

                Total latency: 2-3 seconds";
        }

        // ============================================================================
        // Pattern 2: Time Windowing
        // ============================================================================
        /// <summary>
        /// Divide stream into time windows for aggregation
        /// Windows: Tumbling, Hopping, Sliding, Session
        /// </summary>
        public static class TimeWindowing
        {
            public const string WINDOW_TYPES = @"
                1. TUMBLING WINDOW (Non-overlapping)
                   ├─ [10:00-10:05)  ├─ [10:05-10:10)  ├─ [10:10-10:15)
                   Query: Sum orders every 5 minutes
                   SELECT SUM(amount) FROM stream GROUP BY TumblingWindow(s, 5)
                   Use: Exactly once processing (no overlap)

                2. HOPPING WINDOW (Overlapping)
                   ├─ [10:00-10:05)
                        ├─ [10:02-10:07)
                             ├─ [10:04-10:09)
                   Query: Sum with 5min window, every 2min
                   SELECT SUM(amount) FROM stream GROUP BY HoppingWindow(s, 5, 2)
                   Use: Detect trends over shifting windows

                3. SLIDING WINDOW (Continuous)
                   Always: Last 5 minutes of data
                   Query: Real-time moving average
                   SELECT AVG(price) FROM stream GROUP BY SlidingWindow(s, 5)
                   Use: Real-time monitoring (last N minutes)

                4. SESSION WINDOW (Time gaps)
                   ├─ [Click1,Click2,Click3]─5min gap─[Click4,Click5]
                   Query: Group user clicks (session = 5min inactivity)
                   SELECT COUNT(*) FROM stream GROUP BY SessionWindow(s, 5)
                   Use: User session analysis

                Choosing Window:
                ✓ Tumbling: Non-overlapping batches
                ✓ Hopping: Trending analysis
                ✓ Sliding: Real-time moving stats
                ✓ Session: User behavior analysis";

            public const string EXAMPLE = @"
                Scenario: Monitor temperature sensor every 30 seconds

                Data stream:
                10:00:00 - 22°C
                10:00:05 - 23°C
                10:00:10 - 24°C
                10:00:15 - 25°C
                10:00:20 - 26°C
                ...

                TUMBLING WINDOW (30 second):
                [10:00:00-10:00:30): AVG = 24.5°C
                [10:00:30-10:01:00): AVG = 26.2°C
                → 2 results per minute

                SLIDING WINDOW (last 30 seconds):
                10:00:15: AVG(22,23,24) = 23°C
                10:00:20: AVG(23,24,25) = 24°C
                10:00:25: AVG(24,25,26) = 25°C
                → Continuous average (smoother)";
        }

        // ============================================================================
        // Pattern 3: Anomaly Detection
        // ============================================================================
        /// <summary>
        /// Detect unusual patterns in real-time
        /// Use case: Fraud detection, system health monitoring
        /// </summary>
        public async Task<List<AnomalyAlert>> DetectAnomaliesAsync(
            List<SensorReading> readings)
        {
            try
            {
                _logger.LogInformation("→ Running anomaly detection");

                var alerts = new List<AnomalyAlert>();

                // Calculate baseline (normal range)
                double avgTemp = 0;
                foreach (var reading in readings)
                    avgTemp += reading.Temperature;
                avgTemp /= readings.Count;

                double stdDev = 0;
                foreach (var reading in readings)
                    stdDev += (reading.Temperature - avgTemp) * (reading.Temperature - avgTemp);
                stdDev = Math.Sqrt(stdDev / readings.Count);

                // Detect outliers (> 2 std dev from mean)
                foreach (var reading in readings)
                {
                    double zScore = (reading.Temperature - avgTemp) / stdDev;

                    if (Math.Abs(zScore) > 2.5)  // Anomaly threshold
                    {
                        alerts.Add(new AnomalyAlert
                        {
                            Timestamp = reading.Timestamp,
                            SensorId = reading.SensorId,
                            Value = reading.Temperature,
                            Baseline = avgTemp,
                            Deviation = zScore,
                            Severity = Math.Abs(zScore) > 3.5 ? "Critical" : "Warning"
                        });

                        _logger.LogWarning($"⚠ ANOMALY: Sensor {reading.SensorId} = {reading.Temperature}°C " +
                            $"(baseline: {avgTemp:F1}°C, z-score: {zScore:F2})");
                    }
                }

                _logger.LogInformation($"✓ Detected {alerts.Count} anomalies");
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Anomaly detection failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Stream Joins
        // ============================================================================
        /// <summary>
        /// Join two streams: Orders + Customer info
        /// Join window: Match orders to customer data within 1 minute
        /// </summary>
        public static class StreamJoins
        {
            public const string EXAMPLE = @"
                ORDER STREAM:
                10:00:00 - OrderId=123, CustomerId=5, Amount=$100
                10:00:05 - OrderId=124, CustomerId=8, Amount=$50
                10:00:10 - OrderId=125, CustomerId=5, Amount=$200

                CUSTOMER STREAM (slowly changing):
                09:59:00 - CustomerId=5, Name='Alice', Tier='Gold'
                10:00:30 - CustomerId=8, Name='Bob', Tier='Silver'

                STREAM JOIN QUERY:
                SELECT
                    o.OrderId,
                    c.Name,
                    c.Tier,
                    o.Amount
                FROM orders o
                JOIN customers c
                    ON o.CustomerId = c.CustomerId
                    AND DATEDIFF(minute, c, o) BETWEEN 0 AND 60

                RESULT:
                123 - Alice (Gold) - $100
                124 - Bob (Silver) - $50
                125 - Alice (Gold) - $200

                Use:
                ✓ Enrich streaming data with reference data
                ✓ Match events from different sources
                ✓ Correlate user actions";
        }

        // ============================================================================
        // Pattern 5: Real-Time Alerting
        // ============================================================================
        /// <summary>
        /// Trigger alerts based on stream conditions
        /// </summary>
        public async Task<bool> AlertOnThresholdAsync(
            double temperature,
            double alertThreshold)
        {
            try
            {
                if (temperature > alertThreshold)
                {
                    _logger.LogError($"🚨 ALERT: Temperature {temperature}°C exceeds threshold {alertThreshold}°C");

                    // In real scenario, would:
                    // - Send to Service Bus (trigger automation)
                    // - Send email/SMS
                    // - Update Power BI dashboard
                    // - Store in SQL Database

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Alert failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Reference Data (Slowly Changing Dimensions)
        // ============================================================================
        public static class ReferenceData
        {
            public const string CONCEPT = @"
                STREAMING DATA (fast changing):
                - Sensor readings (continuous)
                - User clicks (frequent)
                - Transactions (frequent)

                REFERENCE DATA (slowly changing):
                - Customer list (daily update)
                - Product catalog (weekly update)
                - Exchange rates (hourly update)

                PATTERN: Join stream to reference data
                - Stream data: OrderId, ProductId, CustomerId
                - Reference data: ProductId → ProductName
                - Output: OrderId, ProductName, CustomerId

                How it works:
                1. Load reference data (Blob, SQL)
                2. Stream Analytics keeps in memory
                3. Each event joins against reference
                4. Periodically reload reference data

                Benefits:
                ✓ Enrich streaming data
                ✓ Translate IDs to names
                ✓ Apply master data";

            public const string EXAMPLE_QUERY = @"
                Load reference data:
                SELECT
                    o.OrderId,
                    p.ProductName,      ← From reference
                    o.Quantity,
                    p.Price * o.Quantity as Total
                FROM orders o
                JOIN products p
                    ON o.ProductId = p.ProductId
                    AND DATEDIFF(minute, p, o) BETWEEN 0 AND 60";
        }

        // ============================================================================
        // Pattern 7: Performance Considerations
        // ============================================================================
        public static class PerformanceTips
        {
            public const string OPTIMIZATION = @"
                1. STREAMING UNITS (Throughput)
                   1 unit: 1MB/second input
                   10 units: 10MB/second input
                   100 units: 100MB/second input
                   Cost: ~$0.11 per unit per hour

                2. LATENCY TUNING
                   Goal: Lower latency = less data buffering
                   Trade-off: Lower latency = higher cost
                   Typical: 2-5 seconds (good balance)

                3. WINDOW SIZE
                   Smaller window: Lower latency (more output)
                   Larger window: Better aggregation (less output)

                4. PARTITION STRATEGY
                   Partition by hot key (reduces contention)
                   Example: Stream by CustomerId (if uneven distribution)

                5. RESOURCE ALLOCATION
                   Monitor: CPU, memory, input lag
                   Scale up if input lag increasing
                   Input lag > 10 seconds = scale up!";

            public const string MONITORING = @"
                Key metrics to monitor:
                ✓ Input lag (how far behind real-time)
                ✓ Late events (% of events arriving late)
                ✓ CPU usage (% utilization)
                ✓ Memory usage (% utilization)
                ✓ Backlogged input events (queue depth)

                Alerts to set:
                ⚠ Input lag > 5 seconds: Scale up
                ⚠ CPU > 80%: Scale up
                ⚠ Memory > 80%: Reduce window size";
        }

        // ============================================================================
        // Pattern 8: Stream Analytics vs Other Technologies
        // ============================================================================
        public static class TechnologyComparison
        {
            public const string COMPARISON = @"
                STREAM ANALYTICS (Serverless, Managed):
                - Setup: Minutes (SQL-like queries)
                - Scaling: Automatic (streaming units)
                - Cost: $0.11/unit/hour = ~$80/month base
                - Latency: 2-5 seconds
                - Use: Monitoring, alerting, simple processing
                - Strength: Ease of use, no ops burden

                APACHE KAFKA + SPARK STREAMING:
                - Setup: Days/weeks (infrastructure)
                - Scaling: Manual (cluster management)
                - Cost: $1-5/hour (compute + storage)
                - Latency: Sub-second
                - Use: Complex processing, high throughput
                - Strength: Flexibility, performance

                AZURE EVENT HUBS + FUNCTIONS:
                - Setup: Hours (code-based)
                - Scaling: Automatic (serverless)
                - Cost: $0.02-0.04/event + compute
                - Latency: 1-2 seconds
                - Use: Event routing, lightweight processing
                - Strength: Cost, tight integration

                DECISION FRAMEWORK:
                Use Stream Analytics if:
                ✓ SQL-like queries sufficient
                ✓ Want managed service
                ✓ Simple transformations

                Use Kafka/Spark if:
                ✓ Complex processing needed
                ✓ High throughput (GB/s)
                ✓ Sub-second latency required

                Use Functions if:
                ✓ Simple event processing
                ✓ Per-event logic
                ✓ Want minimal cost";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public string SensorId { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
    }

    public class AnomalyAlert
    {
        public DateTime Timestamp { get; set; }
        public string SensorId { get; set; }
        public double Value { get; set; }
        public double Baseline { get; set; }
        public double Deviation { get; set; }
        public string Severity { get; set; }
    }
}
