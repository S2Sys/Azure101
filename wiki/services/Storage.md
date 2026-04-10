# Azure Storage Services - Complete Wiki

## Overview

Azure Storage provides massively scalable cloud storage for unstructured data. Four main services: Blob Storage (objects), File Shares (SMB/NFS), Queue Storage (messages), Table Storage (NoSQL).

---

## Quick Comparison

| Service | Use Case | Throughput | Latency | Cost |
|---------|----------|-----------|---------|------|
| **Blob** | Objects, archives, backups | 60GB+/sec | Low | Low |
| **Files** | File shares, SMB/NFS | 1GB/sec | Low | Medium |
| **Queue** | Async messaging | 2000 msg/sec | Low | Low |
| **Table** | Semi-structured data | Variable | Low | Low |
| **ADLS Gen2** | Big data, Hadoop | 60GB+/sec | Low | Low |

---

## 1. Azure Blob Storage

### What is it?
Object storage for unstructured data (documents, images, backups, logs).

### Core Features
- **Hot Tier**: Frequent access (~$0.018/GB/month)
- **Cool Tier**: 30+ days minimum (~$0.010/GB/month)
- **Archive Tier**: 180+ days minimum (~$0.0015/GB/month)
- **Lifecycle**: Auto-move between tiers
- **Blob Types**: Block (recommended), Page (VHDs), Append (logs)

### Real-World Patterns
1. **Backup & Archive**: Hot (recent) → Cool (backup) → Archive (old)
2. **Content Distribution**: Blob + CDN for global users
3. **Data Lake**: Raw data storage for analytics
4. **Log Storage**: Append blobs for continuous logging

### Performance Tips
- Upload in parallel (4MB chunks)
- Use connection pooling
- Batch operations where possible
- Partition keys for better distribution
- Monitor with Storage Analytics

### Cost Optimization
- Lifecycle policies (30% savings typical)
- Archive tier for old data (90% cheaper)
- LRS if regional redundancy acceptable
- Delete aged data automatically

### Security
- Managed Identity (preferred)
- SAS tokens for temp access
- Storage firewall + private endpoints
- Encryption enabled by default

### Interview Q&A
- **Q**: Difference between Blob tiers?
  - **A**: Hot (frequent, expensive), Cool (30+ days, cheaper), Archive (180+ days, cheapest but slow retrieval)
- **Q**: How to handle 10GB upload?
  - **A**: Split into 4MB chunks, upload in parallel, retry on failure with backoff
- **Q**: Cost optimization for backups?
  - **A**: Move to Cool tier after 30 days, Archive after 90 days, delete after 7 years

---

## 2. Azure Files

### What is it?
Managed file shares with SMB 3.1.1 and NFS 4.1 protocols.

### When to Use
- Shared storage for multiple VMs
- Lift-and-shift legacy apps
- Team file sharing
- Replacing on-premises file servers

### Share Types
- **Standard**: HDD-backed, reliable, up to 100TB
- **Premium**: SSD-backed, high performance, up to 100TB

### Advantages over Blob
- ✅ File system semantics (directories, permissions)
- ✅ SMB/NFS protocol support
- ✅ Mount as network drive
- ✅ POSIX compliance (Linux)

### Limitations
- Throughput: 1GB/sec max (vs 60GB/sec for Blob)
- Cost: ~3-4x more than Blob
- Best for small teams, not massive scale

### Real-World Use Cases
1. **Content Management**: Shared document library
2. **Development**: Shared code/config storage
3. **Media**: Video editing with shared access
4. **Backup**: On-premises file server backup

### Interview Q&A
- **Q**: When to use Files vs. Blob?
  - **A**: Files for shared file system needs (SMB), Blob for object storage (archives, backups)
- **Q**: Performance characteristics?
  - **A**: Files: 1GB/sec, Blob: 60GB/sec - Blob wins for scale

---

## 3. Azure Queue Storage

### What is it?
Simple, reliable message queue for async workloads.

### Queue vs. Service Bus
| Feature | Queue | Service Bus |
|---------|-------|----------|
| **Message Size** | 64 KB | 256 KB |
| **Retention** | 7 days (default) | Configurable |
| **Delivery** | At-least-once | At-least-once |
| **DLQ** | Manual handling | Automatic |
| **Topics/Subscriptions** | No | Yes |
| **Cost** | Cheaper | Higher |

### When to Use Queue
- Simple async job processing
- Low-cost decoupling
- Basic producer-consumer patterns
- When Service Bus features not needed

### Producer-Consumer Pattern
```
Producer: Add message to queue
Consumer: Lease message, process, delete
```

### Scaling Considerations
- Single queue: Up to 2000 msg/sec
- Partition into multiple queues for higher throughput
- Use visibility timeout to prevent duplicates
- Monitor queue length (high = slow processing)

### Real-World Scenarios
1. **Image Processing**: Upload image → Queue → Function resizes
2. **Email Notifications**: Enqueue email → Worker sends async
3. **Background Jobs**: Long-running jobs in background

### Interview Q&A
- **Q**: Queue vs. Service Bus?
  - **A**: Use Queue for simple scenarios, Service Bus for enterprise messaging with topics/subscriptions
- **Q**: How to scale beyond 2000 msg/sec?
  - **A**: Partition into multiple queues, use Service Bus for higher throughput

---

## 4. Azure Table Storage

### What is it?
NoSQL key-value store for semi-structured data (2TB per table).

### Data Model
- **Table**: Collection of entities
- **Partition Key**: Logical grouping (e.g., UserId)
- **Row Key**: Unique within partition (e.g., Date)
- **Properties**: Flexible attributes

### When to Use
- Session storage (fast, cheap)
- Device telemetry (IoT)
- User preferences/settings
- Historical data (events, logs)

### When NOT to Use
- Complex queries (limited filtering)
- Transactions across partitions
- Large scale (prefer Cosmos DB)
- Relational data (use SQL)

### Performance Optimization
- **Partition Key Design**: Critical for performance
  - Avoid hot partitions (don't all use same partition key)
  - Distribute evenly across partitions
  - Example: UserId + Date as composite key
- **Indexing**: Only partition and row keys indexed
- **Batch Operations**: 100 entities max per batch

### Real-World Use Cases
1. **Sessions**: UserId partition, SessionId row key
2. **IoT**: DeviceId partition, Timestamp row key
3. **Activity Log**: UserId partition, Date row key
4. **Configuration**: AppName partition, Setting row key

### Interview Q&A
- **Q**: When to use Table vs. Cosmos DB?
  - **A**: Table for simple, structured data, cheap. Cosmos DB for global, flexible schema, complex queries.
- **Q**: How to design partition key?
  - **A**: Use high-cardinality attribute (UserId), distribute evenly, avoid hot partitions

---

## 5. Data Lake Storage (ADLS Gen2)

### What is it?
Blob Storage with hierarchical namespace for big data analytics.

### Differences from Blob
- **Hierarchical Namespace**: Folder structure (vs. flat Blob)
- **Hadoop Compatibility**: Works with Spark, Hive, Presto
- **Access Control**: File/directory level permissions
- **Cost**: Similar to Blob (~5-10% more)

### When to Use
- Big data analytics (Spark, Hadoop)
- Machine learning data lakes
- Complex folder hierarchies
- Hadoop ecosystem integration

### Real-World Architecture
```
Raw Data (Bronze Layer)
    ↓ Transform
Refined Data (Silver Layer)
    ↓ Transform
Analytics Ready (Gold Layer)
    ↓
Power BI / Tableau Reports
```

### Interview Q&A
- **Q**: Blob Storage vs. Data Lake Storage?
  - **A**: ADLS Gen2 for hierarchical namespace and Hadoop compatibility, Blob for flat object storage

---

## Storage Architecture Patterns

### Pattern 1: Time-Series Data
```
Blob Container: historical-logs
├─ 2026/01/01/00-01.log
├─ 2026/01/01/01-02.log
└─ Lifecycle: Move to Archive after 90 days
```

### Pattern 2: Multi-Tier Backup
```
Hot: Current backups (30 days)
Cool: Monthly backups (90 days)
Archive: Annual backups (7 years)
Lifecycle: Automatic transitions
```

### Pattern 3: Shared Team Storage
```
Azure Files (SMB)
├─ Sales folder (accessible by sales team)
├─ Engineering folder (accessible by engineers)
└─ Everyone accessible
```

---

## Cost Optimization Strategies

1. **Lifecycle Policies**: 30-70% savings
2. **Archive Tier**: 90%+ cheaper for old data
3. **Redundancy Tuning**: LRS vs. GRS (50% diff)
4. **Data Retention**: Delete obsolete data
5. **Access Patterns**: Monitor hot/cold data

### Cost Reduction Checklist
- [ ] Implement lifecycle policies
- [ ] Archive data > 90 days old
- [ ] Delete data > 7 years old
- [ ] Use LRS for non-critical data
- [ ] Monitor access patterns

---

## Security Best Practices

1. **Authentication**: Managed Identity > Keys
2. **Authorization**: RBAC for access control
3. **Network**: Private endpoints, firewall rules
4. **Encryption**: Enabled by default (TLS + at-rest)
5. **Monitoring**: Enable audit logging

---

## Interview Questions Summary

**Easy (Beginner)**:
- What are storage tiers and when to use each?
- What's the difference between Blob and File Shares?
- How do lifecycle policies work?

**Medium (Intermediate)**:
- Design a backup strategy for 50TB data
- Optimize storage costs for archival data
- Implement row-level security in Table Storage

**Hard (Advanced)**:
- Design multi-region, multi-tier data lake
- Optimize partition key for 1B entities in Table Storage
- Handle hot partition problem in Blob Storage

---

**Last Updated**: April 2026  
**Status**: Complete  
**Difficulty**: Beginner to Advanced
