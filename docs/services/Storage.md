# Azure Storage Services

Azure Storage is a massively scalable cloud storage solution for modern data storage scenarios. It provides highly available, massively scalable, durable, and secure storage for a variety of data objects in the cloud.

## Overview

Azure Storage includes four main services:

### 1. **Azure Blob Storage** (Object Storage)
For unstructured data like documents, images, videos, and backups.
- **Use for**: Archives, backups, media, data lakes
- **Storage types**: Block blobs (up to 4.75 TB), Page blobs (VHDs), Append blobs (logs)
- **Tiers**: Hot (frequent access), Cool (30+ days), Archive (rarely accessed)

### 2. **Azure File Shares** (Managed File System)
For SMB/NFS file sharing with standard file system semantics.
- **Use for**: File sharing, shared storage for VMs, legacy app migration
- **Protocols**: SMB 3.1.1, NFS 4.1
- **Quota**: Up to 100 TiB per share

### 3. **Azure Queue Storage** (Simple Messaging)
For reliable asynchronous message queuing between application components.
- **Use for**: Decoupling applications, async processing, job queues
- **Message size**: Up to 64 KB
- **Retention**: 7 days default

### 4. **Azure Table Storage** (NoSQL)
For structured semi-relational data with flexible schema.
- **Use for**: Telemetry, device data, session storage, time-series data
- **Partition key**: Logical grouping (important for performance)
- **Row key**: Unique identifier within partition

## Core Concepts

### Storage Accounts
- Container for all your storage data
- Provides unique namespace (URL)
- Redundancy options: LRS, ZRS, GRS, GZRS
- Access tiers: Hot, Cool, Archive

### Redundancy Options

| Option | Copies | Scope | Best For |
|--------|--------|-------|----------|
| **LRS** | 3 | Single region | Dev/test, non-critical data |
| **ZRS** | 3 | Availability zones | High availability in region |
| **GRS** | 6 | Two regions | Disaster recovery scenarios |
| **GZRS** | 6 | Zones + regions | Maximum availability |

### Access Control
- **Managed Identity**: Recommended for Azure services
- **Storage Account Keys**: Easy but less secure, use Key Vault if needed
- **Shared Access Signature (SAS)**: Time-limited, permission-specific access
- **Role-Based Access Control (RBAC)**: Fine-grained permissions

## Pros and Cons

### Pros ✅
- **Massive Scale**: Petabytes of data supported
- **Cost Effective**: Particularly with lifecycle policies and Archive tier
- **High Availability**: Multiple redundancy options with SLAs up to 99.99%
- **Flexible Access**: Multiple protocols (HTTPS, SMB, NFS)
- **Integration**: Seamless integration with Azure services
- **Tiering**: Automatic cost optimization with lifecycle policies
- **Serverless**: No infrastructure to manage

### Cons ❌
- **Eventual Consistency**: GRS replication is asynchronous
- **Basic Query**: Limited query capabilities (no complex filtering like SQL)
- **File Size Limits**: Individual blobs max 4.75 TB
- **Cold Storage Latency**: Archive tier has retrieval delays (can be hours)
- **Transaction Limits**: Limited to single partition transactions for Tables
- **Complexity**: Multiple services means learning multiple APIs

## Real-World Patterns

### 1. **Data Lake Architecture**
Store raw data in Data Lake Storage Gen2, process with Spark/Hadoop, organized in bronze-silver-gold tiers.
```
Raw Data (Bronze) → Cleaned Data (Silver) → Analytics Ready (Gold)
```

### 2. **Backup & Disaster Recovery**
- Store backups in GRS with Archive tier
- Use lifecycle policies to age off backups
- Reduces RTO/RPO concerns

### 3. **Content Delivery**
- Store static content (images, videos) in Blob Storage
- Use CDN for global distribution
- Reduces bandwidth costs and latency

### 4. **Telemetry & Logging**
- Stream telemetry to Append blobs
- Organize by date partition (year/month/day)
- Move to Archive after retention period

### 5. **Producer-Consumer Pattern**
- Producer writes to Queue Storage
- Multiple workers dequeue and process
- Scales independently

### 6. **Session Storage**
- Use Table Storage for lightweight session data
- Partition by user ID for performance
- Cheap alternative to Redis for simple scenarios

## Performance Tips

### Blob Storage
- **Parallelism**: Upload blobs in parallel for better throughput
- **Batch Operations**: Use batch operations where available
- **Connection Pooling**: Reuse client objects
- **Pagination**: Use pagination for listing large containers
- **Size Matters**: Larger files = better throughput

### File Shares
- **Network**: Ensure good network connectivity (SMB over internet is slower)
- **Caching**: Use Azure File Sync for hybrid scenarios
- **Throughput**: Shares have throughput limits, monitor with metrics

### Queue Storage
- **Visibility Timeout**: Set appropriate timeout (prevent duplicate processing)
- **Backoff**: Use exponential backoff when queue is empty
- **Scaling**: Consider Service Bus for better throughput

### Table Storage
- **Partition Key Design**: Critical for performance - spread evenly
- **Row Key**: Sort by range for range queries
- **Batch Operations**: Use batch transactions within partition
- **Indexes**: Only partition and row keys are indexed

## Common Interview Questions

### 1. **Difference between Hot, Cool, and Archive tiers?**
**Answer**: Hot tier is for frequent access with higher storage cost. Cool tier (min 30 days) has lower storage cost but higher access cost. Archive (min 180 days) has lowest cost but high retrieval latency and cost.

### 2. **How do you secure blob access without exposing keys?**
**Answer**: Use managed identities for Azure services, Azure AD/RBAC for users, or Shared Access Signatures (SAS) for temporary access. Never put keys in code; store in Key Vault if needed.

### 3. **When would you use Table Storage vs. Cosmos DB?**
**Answer**: Table Storage for simple, structured, semi-relational data (telemetry, sessions). Cosmos DB for complex queries, complex relationships, or multi-region requirements. Cosmos DB is more capable but pricier.

### 4. **What's the difference between Blob Storage and Data Lake Storage?**
**Answer**: Data Lake Storage Gen2 adds hierarchical namespace (directory support) on top of Blob Storage. Use ADLS Gen2 for big data analytics and Hadoop compatibility, Blob Storage for general object storage.

### 5. **How do you optimize Azure Storage costs?**
**Answer**: Use lifecycle policies to move data to cheaper tiers, use appropriate redundancy (LRS for non-critical), archive old data, monitor access patterns, and clean up unused storage.

### 6. **Explain Shared Access Signatures (SAS) tokens**
**Answer**: SAS tokens provide time-limited, permission-specific access to storage without exposing account keys. Can be scoped to blobs, containers, or accounts. Always set expiration and restrict permissions to minimize blast radius.

## Code Examples

See `src/Azure101.Storage/Examples/` for complete code examples:
- `BlobStorageBasicExample.cs` - Upload, download, delete, list blobs
- `BlobStorageAdvancedExample.cs` (coming soon) - Snapshots, tiers, SAS tokens
- `FileShareExample.cs` (coming soon) - File operations, mounting
- `QueueStorageExample.cs` (coming soon) - Producer-consumer pattern
- `TableStorageExample.cs` (coming soon) - CRUD, batch operations

## Architecture Decision Guide

```
Unstructured data (images, logs, documents)?
├─ Yes → Blob Storage
│   ├─ Access frequently? → Hot tier
│   ├─ Access occasionally? → Cool tier
│   └─ Archive/rare access? → Archive tier
└─ No → File sharing needs? → Azure Files
    ├─ SMB/NFS required? → Yes → File Shares
    └─ No → Structured semi-relational? → Table Storage
```

## Next Steps

1. **Get Hands-On**: Review and run code examples
2. **Study Interview Q&A**: Understand differences between services
3. **Practice**: Create a sample application using multiple storage services
4. **Optimize**: Design lifecycle policies for your data
5. **Secure**: Implement secure authentication patterns

## Additional Resources

- [Azure Storage Documentation](https://docs.microsoft.com/azure/storage/)
- [Azure Storage Best Practices](https://docs.microsoft.com/azure/storage/common/storage-best-practices)
- [Azure Storage Pricing](https://azure.microsoft.com/pricing/details/storage/)
- [Azure Well-Architected - Storage Pillar](https://docs.microsoft.com/azure/architecture/framework/services/storage)
