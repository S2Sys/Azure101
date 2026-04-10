using Azure101.Documentation.Models;

#nullable enable

namespace Azure101.Storage.InterviewContent;

/// <summary>
/// Interview questions and answers for Azure Storage services
/// Covers Blob Storage, File Shares, Queues, Tables, and Data Lake Storage
/// </summary>
public static class StorageInterviewQA
{
    public static List<InterviewQuestion> GetBlobStorageQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What are the differences between Azure Blob Storage hot, cool, and archive tiers?",
            Answer = "Hot tier is for frequently accessed data with higher storage costs but lower access costs. Cool tier is for infrequently accessed data (at least 30 days) with lower storage costs but higher access costs. Archive tier is for rarely accessed data with the lowest storage cost but highest access cost and retrieval time (can be hours).",
            DetailedExplanation = "Choose tiers based on access patterns: hot for active data (logs, images in use), cool for backup/occasional access, archive for long-term retention/compliance. Microsoft recommends moving data between tiers as usage patterns change.",
            Category = "Storage Tiers",
            Difficulty = InterviewDifficulty.Beginner,
            RelatedTopics = new[] { "Cost optimization", "Lifecycle policies", "Rehydration" },
            Tags = new[] { "storage-tier", "cost", "fundamental" }
        },

        new InterviewQuestion
        {
            Question = "How do you configure lifecycle management policies for blobs?",
            Answer = "Use Azure Portal, Azure CLI, or Azure SDKs to create lifecycle management policies that automatically transition or delete blobs based on age or access patterns. Policies are defined in JSON and can move blobs between tiers or delete them after X days.",
            DetailedExplanation = "Example: Move blobs to cool tier after 30 days of creation, move to archive after 90 days, delete after 365 days. This reduces costs significantly for data with predictable access patterns. Policies are evaluated once daily.",
            Category = "Management",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Cost optimization", "Automation", "Retention" },
            Tags = new[] { "lifecycle", "automation", "cost-optimization" }
        },

        new InterviewQuestion
        {
            Question = "What is the difference between Blob Storage and Azure Data Lake Storage (ADLS)?",
            Answer = "ADLS Gen2 is built on top of Blob Storage and adds hierarchical namespace support (like a traditional file system), fine-grained access control, and optimizations for big data analytics. ADLS is better for Hadoop-compatible workloads and analytics.",
            DetailedExplanation = "Both use the same underlying Blob Storage, but ADLS Gen2 adds directory structure and permissions. Choose Blob Storage for general-purpose unstructured data, ADLS Gen2 for big data analytics, Spark jobs, and data lakes.",
            Category = "Service Comparison",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Hierarchical namespace", "Big data", "Analytics" },
            Tags = new[] { "adls", "blob-storage", "comparison" }
        },

        new InterviewQuestion
        {
            Question = "How do you ensure secure access to blobs without exposing storage account keys?",
            Answer = "Use managed identities for authentication instead of storage account keys, implement Shared Access Signatures (SAS) for time-limited access, use Azure role-based access control (RBAC), or enable Azure AD authentication. Store keys in Azure Key Vault if needed.",
            DetailedExplanation = "Best practice: Use DefaultAzureCredential with managed identity in production. For temporary access, use SAS tokens with specific permissions and expiration. Never share storage account keys or put them in code.",
            Category = "Security",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Managed Identity", "RBAC", "SAS tokens", "Key Vault" },
            Tags = new[] { "security", "authentication", "best-practice" }
        },

        new InterviewQuestion
        {
            Question = "What is a Shared Access Signature (SAS) and when would you use it?",
            Answer = "A SAS is a time-limited, permission-specific URL that grants access to a storage resource without exposing the account key. Use it for temporary access by external users, batch downloads, or scenarios where you need fine-grained control over permissions.",
            DetailedExplanation = "SAS tokens can be account-level or resource-level (blob/container-level). Always set an expiration time. You can restrict permissions (read-only, write, delete) and IP addresses. Revoke tokens by regenerating keys.",
            Category = "Security",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Authentication", "Access control", "Time-limited access" },
            Tags = new[] { "sas", "security", "sharing-data" }
        },

        new InterviewQuestion
        {
            Question = "How do you handle large blob uploads efficiently?",
            Answer = "Use block blob uploads with BlobClient.UploadAsync() for concurrent chunk uploads, or use BlobClient.StageBlockAsync() and CommitBlockListAsync() for more control. For very large files, consider using Azure Storage for parallel uploads or Azure Data Box for offline transfer.",
            DetailedExplanation = "Block blobs (default) support up to 4.75 TB and can be uploaded in 4 MB chunks in parallel. Page blobs are optimized for random I/O (VMs). Append blobs are for append-only scenarios (logs). Choose based on your use case.",
            Category = "Performance",
            Difficulty = InterviewDifficulty.Advanced,
            RelatedTopics = new[] { "Blob types", "Chunking", "Parallelization" },
            Tags = new[] { "performance", "upload", "optimization" }
        }
    };

    public static List<InterviewQuestion> GetFileShareQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What is the difference between Azure Files and Azure Blob Storage?",
            Answer = "Azure Files provides SMB/NFS file sharing protocol support for lift-and-shift scenarios, shared file access from multiple VMs, and Windows file share compatibility. Blob Storage is for unstructured object storage without file share protocols. Files has lower throughput but better compatibility.",
            DetailedExplanation = "Use Azure Files when you need file share semantics (multiple clients, SMB protocol, network drive mounting). Use Blob Storage for general data storage, archives, and analytics. Files supports both SMB and NFS protocols.",
            Category = "Service Comparison",
            Difficulty = InterviewDifficulty.Beginner,
            RelatedTopics = new[] { "SMB", "NFS", "File sharing" },
            Tags = new[] { "file-share", "comparison", "protocol" }
        },

        new InterviewQuestion
        {
            Question = "When would you choose Azure Files over Blob Storage for an application?",
            Answer = "Choose Files when: you need POSIX-compliant file permissions, multiple users/VMs need simultaneous read/write access, you're migrating legacy apps expecting file share semantics, you need SMB or NFS protocol support, or you want to mount storage as a network drive.",
            DetailedExplanation = "Files is great for shared team storage, content management systems, and applications expecting traditional file systems. Blob is better for unstructured data, analytics, archives, and container-based apps.",
            Category = "Architecture",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Use cases", "Protocol support", "Access patterns" },
            Tags = new[] { "file-share", "architecture-decision" }
        }
    };

    public static List<InterviewQuestion> GetQueueStorageQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What is the difference between Azure Queue Storage and Azure Service Bus?",
            Answer = "Queue Storage is simple, cost-effective FIFO messaging for basic async scenarios. Service Bus is enterprise messaging with advanced features: message sessions, dead-letter queues, scheduled delivery, topic subscriptions, and AMQP support. Choose Queue for simple scenarios, Service Bus for complex enterprise needs.",
            DetailedExplanation = "Queue Storage: max 64 KB message size, 7-day retention, simple FIFO. Service Bus: max 256 KB, longer retention, duplicate detection, transactions, and competing consumers pattern.",
            Category = "Service Comparison",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Messaging patterns", "Enterprise messaging" },
            Tags = new[] { "queue-storage", "service-bus", "comparison" }
        },

        new InterviewQuestion
        {
            Question = "How do you implement a producer-consumer pattern using Queue Storage?",
            Answer = "Producer enqueues messages using PutMessage() or InsertMessageAsync(). Consumer retrieves messages using GetMessages() or ReceiveMessagesAsync(), processes them, then deletes with DeleteMessage() or DeleteMessageAsync(). Use visibility timeout to prevent other consumers from processing.",
            DetailedExplanation = "Set appropriate visibility timeout (how long message is hidden from other consumers while being processed). Use exponential backoff when polling. Handle message processing idempotently in case of retries. Consider using dequeue count for dead-lettering.",
            Category = "Patterns",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Producer-consumer", "Async processing" },
            Tags = new[] { "queue-storage", "pattern", "async" }
        }
    };

    public static List<InterviewQuestion> GetTableStorageQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What is Azure Table Storage and when would you use it?",
            Answer = "Table Storage is a NoSQL data store for storing structured semi-relational data using Azure Tables API. It's useful for semi-structured data, time-series data, device telemetry, and scenarios where you need flexibility without a relational schema. Cost-effective alternative to SQL for some workloads.",
            DetailedExplanation = "Tables are organized by partition key and row key. Data is semi-structured with variable properties per entity. Good for telemetry, device data, sessions. Not suitable for complex queries or transactions across partitions.",
            Category = "Overview",
            Difficulty = InterviewDifficulty.Beginner,
            RelatedTopics = new[] { "NoSQL", "Partitioning", "Semi-structured data" },
            Tags = new[] { "table-storage", "nosql", "use-cases" }
        }
    };

    public static List<InterviewQuestion> GetCommonStorageQuestions() => new()
    {
        new InterviewQuestion
        {
            Question = "What are the redundancy options in Azure Storage and when would you use each?",
            Answer = "LRS (Local Redundant Storage): 3 copies in one region. ZRS (Zone-Redundant): 3 copies across availability zones. GRS (Geo-Redundant): 6 copies (3 in primary, 3 in secondary region). GZRS (Geo-Zone-Redundant): 6 copies across zones and regions. Choose based on availability and disaster recovery needs.",
            DetailedExplanation = "LRS is cheapest but no disaster recovery. ZRS protects against datacenter failures. GRS provides geographic redundancy with automatic failover. GZRS offers maximum availability. Read-access GRS/GZRS allow reading from secondary region.",
            Category = "Durability & Availability",
            Difficulty = InterviewDifficulty.Intermediate,
            RelatedTopics = new[] { "Redundancy", "Disaster recovery", "SLA" },
            Tags = new[] { "redundancy", "availability", "sla" }
        },

        new InterviewQuestion
        {
            Question = "How do you optimize storage costs?",
            Answer = "Use appropriate redundancy level for your needs, implement lifecycle policies to move data to cheaper tiers, delete unused data, use blob snapshots instead of full copies, enable storage analytics to identify expensive access patterns, and consider using Archive tier for long-term retention.",
            DetailedExplanation = "Typical savings: lifecycle policies (30-50%), right-sizing redundancy (20-30%), Archive tier for backups (90%+). Monitor costs using Azure Cost Management. Balance cost with availability/durability requirements.",
            Category = "Cost Optimization",
            Difficulty = InterviewDifficulty.Advanced,
            RelatedTopics = new[] { "Lifecycle policies", "Tier selection", "Cost analysis" },
            Tags = new[] { "cost-optimization", "strategy", "advanced" }
        }
    };

    /// <summary>
    /// Gets all storage interview questions
    /// </summary>
    public static List<InterviewQuestion> GetAllQuestions()
    {
        var allQuestions = new List<InterviewQuestion>();
        allQuestions.AddRange(GetBlobStorageQuestions());
        allQuestions.AddRange(GetFileShareQuestions());
        allQuestions.AddRange(GetQueueStorageQuestions());
        allQuestions.AddRange(GetTableStorageQuestions());
        allQuestions.AddRange(GetCommonStorageQuestions());
        return allQuestions;
    }
}
