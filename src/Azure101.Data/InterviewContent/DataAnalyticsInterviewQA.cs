using System;
using System.Collections.Generic;

namespace Azure101.DataAnalytics.InterviewContent
{
    public static class DataAnalyticsInterviewQA
    {
        public static List<(string Question, string Answer, string Category)> GetQuestions() => new()
        {
            ("Design a data warehouse for 1 billion transactions/day",
            "1. Ingestion: Data Factory (ETL) pulls from OLTP database (nightly). 2. Raw storage: Data Lake Storage (ADLS) - Bronze layer. 3. Processing: Databricks cleans, deduplicates, validates (Silver layer). 4. Warehouse: Synapse SQL loads dimensional and fact tables (Gold layer). 5. BI: Power BI queries Gold layer. 6. Cost: $5000-10000/month (Synapse + compute).",
            "Data Warehouse"),

            ("Implement real-time dashboard for IoT data (1M sensors)",
            "1. IoT devices → Event Hubs (1M events/sec). 2. Stream Analytics: Aggregate per location, 1-minute window. 3. Output: Azure SQL (real-time aggregates). 4. Power BI: Auto-refresh, connected to SQL. 5. Dashboard: Show last hour, alerts if anomaly. 6. Cost: ~$2000-5000/month.",
            "Real-Time Analytics"),

            ("How do you optimize Synapse Analytics queries for fast performance?",
            "1. Distribution: Distribute fact tables on key (user_id, date). 2. Statistics: Create statistics on join columns. 3. Materialized views: Pre-aggregate common queries. 4. Partitioning: Sales by month, query only needed month. 5. Columnar compression: Store data compressed. 6. Avoid: SELECT *, table scans, expensive joins.",
            "Query Optimization"),

            ("Design data lake architecture (Bronze/Silver/Gold)",
            "Bronze: Raw data as-is from sources (no transformation). Silver: Cleaned, validated, deduplicated (business-ready). Gold: Aggregated, denormalized (optimized for BI). Flow: Source → ADLS Bronze (Data Factory) → Databricks → ADLS Silver → Synapse → ADLS Gold → Power BI. Benefit: Separation of concerns, easy to backfill.",
            "Data Architecture"),

            ("Implement data governance and lineage tracking",
            "1. Azure Purview: Catalog all data assets. 2. Lineage: Track data source → transformation → destination. 3. Metadata: Document each dataset (owner, sensitivity, retention). 4. Access control: RBAC on sensitive data (PII). 5. Audit: Log who accessed what, when. 6. Data quality: Run tests nightly, alert if metrics degrade.",
            "Data Governance"),

            ("How do you handle GDPR 'right to be forgotten' for petabyte-scale data?",
            "1. Identify: Find all customer data (across all systems). 2. Baseline: Update customer record (mark as deleted). 3. Database: Hard delete from OLTP. 4. Data Lake: Reprocess historical data, exclude customer. 5. Backups: Expire old backups (< 30 day retention). 6. Verification: Confirm complete deletion. 7. Time: 30-90 days for massive datasets.",
            "Data Privacy")
        };
    }
}
