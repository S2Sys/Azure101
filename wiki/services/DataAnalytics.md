# Azure Data & Analytics Services - Complete Wiki

## Overview

Azure Data & Analytics provides big data processing, data warehousing, and analytics tools. Process massive datasets to extract business insights.

---

## Quick Comparison Table

| Service | Purpose | Throughput | Cost | Best For |
|---------|---------|-----------|------|----------|
| **Data Lake Storage** | Raw data storage | 60GB+/sec | Low | Hadoop, big data |
| **Synapse Analytics** | Data warehouse + spark | Variable | Medium | Enterprise DW, BI |
| **Databricks** | Spark processing | Variable | Medium-High | Data science, ML |
| **Stream Analytics** | Real-time streaming | 1M events/sec | Low | Real-time analytics |
| **HDInsight** | Managed Hadoop/Spark | Variable | Medium | Big data clusters |
| **Data Explorer** | Time-series analytics | 1M+ events/sec | Medium | Logs, metrics, telemetry |

---

## 1. Azure Data Lake Storage (ADLS)

### What is it?
Massive-scale, secure data repository optimized for big data analytics. Stores structured and unstructured data.

### When to Use
- Store raw data for analytics
- Hadoop/Spark processing
- Data lake foundation
- Long-term data retention
- Multi-format data storage

### Key Features
- **Unlimited Scale**: Petabytes of data
- **Hadoop Compatible**: Works with Spark, Hive
- **Security**: POSIX ACLs, encryption
- **Performance**: 60GB+/sec throughput
- **Lifecycle**: Archive old data automatically
- **Compression**: Reduce storage costs

### Architecture & Core Concepts

#### Data Lake Architecture
```
Raw Layer (Bronze)
    ├── Customer data (CSV)
    ├── Transaction logs (JSON)
    ├── Sensor data (Parquet)
    └── No transformations
    
Processed Layer (Silver)
    ├── Cleaned data
    ├── Deduplicated
    ├── Validated
    ├── Standardized schemas
    └── Ready for analytics

Analytics Layer (Gold)
    ├── Aggregated metrics
    ├── Business-ready dashboards
    ├── Data marts
    └── Optimized for reporting
```

### Pros & Cons

**Pros** ✅
- Unlimited scale
- Cost-effective storage (~$5/TB/month)
- Hadoop/Spark compatible
- Security with POSIX ACLs
- Performance (60GB+/sec)
- Compression reduces costs

**Cons** ❌
- Requires processing framework (Spark/Hadoop)
- Not a database (no SQL querying)
- Data management complexity
- Requires expertise

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Account storage | Unlimited |
| File size | 4.75TB max per file |
| Throughput | 60GB+/sec |
| Concurrent connections | 10000+ |

### Real-World Use Cases

#### Use Case 1: Data Warehouse Foundation
```
Raw Data Sources
├── CRM system (daily export)
├── Ecommerce logs (streaming)
├── Website analytics
└── Customer service tickets

Data Lake (ADLS)
├── Store raw (no processing cost)
├── Bronze: Exact copy of source
├── Silver: Cleaned, validated, deduplicated
└── Gold: Aggregated for BI

BI Tools (Power BI)
    └── Query Gold layer
    └── Create dashboards

Cost: ~$100/month storage + compute for processing
```

#### Use Case 2: Machine Learning Training Data
```
Historical Data (1 year)
├── Customer interactions
├── Purchase history
├── Support tickets
└── Total: 50TB uncompressed

Data Lake Storage
├── Store compressed (5TB)
├── Cost: ~$25/month

ML Pipeline
├── Train model on data
├── Achieve 95% accuracy
└── Deploy to production

Cost-effective ML training
```

---

## 2. Azure Synapse Analytics

### What is it?
Unified analytics platform combining data warehouse (SQL) and big data (Spark) in one service.

### When to Use
- Data warehouse for BI
- Large-scale data processing
- Data exploration and analytics
- Real-time and batch together
- Complex analytics queries

### Key Features
- **SQL Pool**: Dedicated SQL data warehouse
- **Spark Pool**: Apache Spark for big data
- **Pipelines**: Data orchestration
- **Studio**: Unified development environment
- **Integration**: Connects to Data Lake Storage

### Architecture & Core Concepts

#### Synapse Architecture
```
Data Sources
├── CSV files in Data Lake
├── Databases
├── APIs
└── Streaming data

Ingest Layer
    ├── Data Factory (ETL)
    └── Data movement

SQL Pool
    ├── Dimensional tables (Date, Customer, Product)
    ├── Fact tables (Sales, Orders)
    ├── MPP architecture (distributed queries)
    └── BI tools query

Spark Pool
    ├── Machine learning
    ├── Data transformation
    ├── Complex analytics
    └── Python/Scala/SQL

Insights
    ├── Power BI dashboards
    ├── Business intelligence
    └── Executive reports
```

### Pros & Cons

**Pros** ✅
- Combined SQL + Spark (one platform)
- MPP architecture (distribute queries)
- Automatic scaling
- Built-in data integration
- Pay for what you use

**Cons** ❌
- Complex to setup and tune
- Expensive for small datasets
- Learning curve (SQL + Spark)
- Cold start on suspend
- Overkill for simple analytics

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| SQL Pool size | Up to 60TB |
| Spark pool nodes | Up to 100+ nodes |
| Query timeout | 24 hours |
| Concurrent queries | 32 |

### Real-World Use Cases

#### Use Case 1: Enterprise Data Warehouse
```
Sales Organization
├── 10 regional offices
├── 100M transactions/year
├── Real-time dashboard needed

Synapse Solution:
├── SQL Pool: Dimension and fact tables
├── Nightly ETL: Load data from regional DBs
├── Spark: Pre-aggregate for faster queries
├── Power BI: Real-time dashboards

Result:
├── CEO sees sales by region/product
├── Dashboard refreshes hourly
├── Supports 100+ concurrent users
```

#### Use Case 2: Customer Analytics
```
Questions to answer:
├── Which customers churning?
├── Product affinity (buy X then Y)?
├── Lifetime value prediction?

Synapse Approach:
├── SQL: Join customer, transaction, product tables
├── Spark: ML models for churn/LTV
├── Results: List of at-risk customers
└── Action: Trigger retention campaigns

Cost: ~$2000/month Synapse + compute
```

---

## 3. Azure Databricks

### What is it?
Managed Apache Spark platform optimized for machine learning and data engineering. Notebooks for interactive development.

### When to Use
- Machine learning projects
- Data science exploration
- Interactive data analysis
- Building data pipelines
- Complex data transformations

### Key Features
- **Notebooks**: Jupyter-like development
- **Clusters**: Auto-scaling Spark clusters
- **ML Runtime**: Pre-installed ML libraries
- **Collaborative**: Shared notebooks and results
- **Integration**: Works with Data Lake, SQL, etc.

### Pros & Cons

**Pros** ✅
- Simple cluster creation
- Great for data scientists
- Collaborative notebooks
- ML-optimized runtime
- Easy Python/Scala/SQL coding

**Cons** ❌
- Expensive (~$0.30-$1/compute hour)
- Not for production pipelines
- Requires Spark knowledge
- Cold start for clusters
- Complex debugging

### Real-World Use Cases

#### Use Case 1: Customer Churn Prediction Model
```
Data Science Team
├── Import data from Data Lake
├── Explore patterns in notebooks
├── Build ML model (XGBoost)
├── Evaluate accuracy (92%)
├── Save model to registry

Production:
├── Deploy model as API
├── Real-time churn scoring
├── Trigger retention campaigns

Development cost: ~$500/month
```

---

## 4. Azure Stream Analytics

### What is it?
Real-time stream processing service. Analyze continuous data streams and generate insights instantly.

### When to Use
- Real-time event processing
- Anomaly detection
- Real-time aggregations
- Dashboard updates
- IoT data processing

### Key Features
- **SQL-like Syntax**: Familiar query language
- **Built-in Functions**: Aggregations, windowing
- **Scaling**: Auto-scale to millions of events/sec
- **Multiple Inputs**: Event Hubs, IoT Hub, Blob Storage
- **Multiple Outputs**: Database, Power BI, Storage

### Architecture & Core Concepts

#### Real-Time Processing Flow
```
Event Source (1M events/sec)
    ├── IoT devices sending temperature
    ├── Events arrive in Event Hubs
    └── Timestamp: Every second

Stream Analytics Query
    ├── Window: Last 1 minute
    ├── Average temperature per location
    ├── Flag if > 30°C (alert)
    └── Count events per location

Output
    ├── Normal: Write to SQL Database
    ├── Alert (> 30°C): Write to alert queue
    └── Dashboard: Real-time visualization
```

### Pros & Cons

**Pros** ✅
- Real-time processing (low latency)
- SQL-like syntax (easier learning)
- Automatic scaling
- Multiple inputs/outputs
- Cost-effective

**Cons** ❌
- Limited to SQL (no complex Python)
- Windowing can be complex
- Stateful processing challenging
- Late arrivals difficult
- Debugging limited

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Throughput | 1M events/sec per unit |
| Input streams | Up to 8 |
| Output sinks | Up to 8 |
| Query timeout | 24 hours |

### Real-World Use Cases

#### Use Case 1: IoT Temperature Monitoring
```
Sensors: 1000 temperature sensors
├── Each sends reading every 10 seconds
├── Total: 100 readings/sec

Stream Analytics:
├── Query: Aggregate per location
├── Window: 1-minute tumbling
├── Alert if > 30°C

Dashboard:
├── Real-time location temperatures
├── Alert overlay
└── Auto-refresh every minute

Cost: ~$20-50/month
```

#### Use Case 2: Website Click Analysis
```
Events: User clicks on website
├── 100k clicks/sec during peak
├── Attributes: user, page, timestamp

Stream Analytics:
├── Count clicks per page per minute
├── Track unique users per page
├── Flag anomalies (unusual traffic)

Power BI:
├── Real-time dashboard
├── Top pages, traffic trends
└── Anomaly alerts

Cost: ~$50-100/month
```

---

## 5. Azure Data Explorer (Kusto)

### What is it?
Fast, fully managed data analytics service optimized for time-series and log data.

### When to Use
- Time-series data (metrics, logs)
- Real-time analytics
- Fast aggregations
- Ad-hoc exploration
- Massive data volumes

### Key Features
- **KQL (Kusto Query Language)**: Optimized for analytics
- **Automatic Indexing**: Fast queries
- **Retention Policies**: Auto-archive old data
- **Ingestion**: High-speed data ingestion (1M+ events/sec)

### Pros & Cons

**Pros** ✅
- Extremely fast queries (1B rows/sec)
- Time-series optimized
- KQL (analytics-focused language)
- Low cost for analytics

**Cons** ❌
- Different query language (KQL)
- Limited transaction support
- Not traditional database
- Learning curve

### Real-World Use Cases

#### Use Case 1: Application Performance Monitoring
```
Source: Application Insights
    └── 1M events/sec
    ├── Request metrics
    ├── Exceptions
    └── Traces

Data Explorer:
    ├── Query: P99 latency by endpoint
    ├── Detect: Performance degradation
    ├── Alert: When P99 > 1 second

Cost: Much cheaper than traditional OLAP
```

---

## Architecture Patterns

### Pattern 1: Modern Data Warehouse
```
Data Ingestion
    ├── ETL via Data Factory
    ├── Streaming via Event Hubs → Stream Analytics
    ├── Batch via Databricks

Data Lake Storage (ADLS)
    ├── Bronze: Raw data
    ├── Silver: Cleaned data
    └── Gold: Aggregated data

Analytics
    ├── Synapse SQL: BI queries
    ├── Databricks: ML models
    └── Stream Analytics: Real-time

Consumption
    ├── Power BI: Executive dashboards
    ├── Looker: Analytical dashboards
    └── Custom apps: Embedded analytics

Cost: $2000-10000/month depending on scale
```

### Pattern 2: Real-Time + Batch Analytics
```
Real-Time Path:
    └── IoT sensors → Event Hubs
    └── Stream Analytics (minute-level)
    └── Power BI (real-time dashboard)

Batch Path:
    └── Historical data in ADLS
    └── Spark processing (hourly)
    └── Write aggregates to database
    └── BI tools query results

Combined:
    ├── Real-time for immediate insights
    ├── Batch for complex analysis
    └── Cost-optimized (real-time on critical, batch for rest)
```

---

## Interview Questions

1. **Design data warehouse for 100M transactions/day**
   - ADLS for raw data storage
   - Data Factory for ETL
   - Synapse SQL Pool for DW
   - Spark for complex transformations
   - Power BI for reporting

2. **Process real-time IoT data from 1M sensors**
   - Event Hubs for ingestion (1M+ events/sec)
   - Stream Analytics for windowing/aggregation
   - Data Explorer for time-series analytics
   - Power BI for dashboards

3. **Build ML pipeline on large dataset**
   - ADLS for data storage
   - Databricks for exploration/ML
   - Train model on 1 year of data
   - Deploy model as API
   - Cost: ~$1000/month

4. **Archive old data and manage storage costs**
   - ADLS Lifecycle: Hot (recent) → Cool (30+ days) → Archive (90+ days)
   - Delete data > 7 years old (compliance met)
   - Compression reduces costs
   - Total: 80% cost reduction

5. **Real-time dashboard for executive team**
   - Stream Analytics: Process events in real-time
   - Power BI: Auto-refresh dashboard
   - Latency: < 1 minute
   - Support 100+ concurrent users

---

## Cost Optimization Tips

1. **Data Lake Storage**: ~$5/TB/month (compress to reduce)
2. **Synapse Analytics**: $1-5 per DWU/hour (right-size)
3. **Stream Analytics**: $0.32/SU/hour (scale appropriately)
4. **Databricks**: $0.30-1/compute hour (use spot instances)
5. **Archive old data**: Move to cool/archive tiers

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Big data, analytics, data warehousing, streaming
