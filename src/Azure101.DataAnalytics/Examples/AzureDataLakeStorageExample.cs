using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.DataLake;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Azure101.DataAnalytics.Examples
{
    /// <summary>
    /// Azure Data Lake Storage (ADLS) - Enterprise Data Repository
    /// Demonstrates: Hierarchical storage, file operations, partitioning, cost optimization
    /// Production pattern: Store petabytes of data for analytics
    /// Benefits: Scalable, secure, optimized for analytics (not file serving)
    /// </summary>
    public class AzureDataLakeStorageExample
    {
        private readonly DataLakeFileSystemClient _fileSystemClient;
        private readonly ILogger<AzureDataLakeStorageExample> _logger;

        public AzureDataLakeStorageExample(
            string storageAccountName,
            string fileSystemName,
            ILogger<AzureDataLakeStorageExample> logger)
        {
            _logger = logger;

            // Connect to ADLS
            var uri = new Uri($"https://{storageAccountName}.dfs.core.windows.net/{fileSystemName}");
            var credential = new DefaultAzureCredential();
            var client = new DataLakeFileSystemClient(uri, credential);
            _fileSystemClient = client;
        }

        // ============================================================================
        // Pattern 1: Hierarchical Directory Structure
        // ============================================================================
        /// <summary>
        /// Organize data in folders (unlike Blob Storage which is flat)
        /// Benefits: Easy navigation, natural partitioning
        /// Example: /raw/2024/01/15/events.parquet
        /// </summary>
        public async Task<bool> CreateDirectoryStructureAsync()
        {
            try
            {
                _logger.LogInformation("→ Creating directory structure");

                // Create directories for data organization
                var year = DateTime.UtcNow.Year;
                var month = DateTime.UtcNow.Month;
                var day = DateTime.UtcNow.Day;

                var pathRaw = $"raw/{year}/{month:D2}/{day:D2}";
                var pathProcessed = $"processed/{year}/{month:D2}/{day:D2}";
                var pathArchive = $"archive/{year}/{month:D2}";

                // Create raw data directory
                await _fileSystemClient.CreateDirectoryAsync(pathRaw);
                _logger.LogInformation($"  ✓ Created: {pathRaw}");

                // Create processed data directory
                await _fileSystemClient.CreateDirectoryAsync(pathProcessed);
                _logger.LogInformation($"  ✓ Created: {pathProcessed}");

                // Create archive directory
                await _fileSystemClient.CreateDirectoryAsync(pathArchive);
                _logger.LogInformation($"  ✓ Created: {pathArchive}");

                _logger.LogInformation($"✓ Directory structure created");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Directory creation failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Upload Files (Batch Data)
        // ============================================================================
        /// <summary>
        /// Upload large data files efficiently
        /// Format: Parquet (columnar, compresses 10:1)
        /// Cost: Store, not serving - very cheap ($0.03/GB/month)
        /// </summary>
        public async Task<bool> UploadDataFileAsync(
            string localFilePath,
            string remoteDirectoryPath,
            string fileName)
        {
            try
            {
                _logger.LogInformation($"→ Uploading {fileName}");

                // Create directory if not exists
                var directoryClient = _fileSystemClient.GetDirectoryClient(remoteDirectoryPath);
                await directoryClient.CreateIfNotExistsAsync();

                // Get file client
                var fileClient = directoryClient.GetFileClient(fileName);

                // Upload file
                await using (var stream = File.OpenRead(localFilePath))
                {
                    await fileClient.UploadAsync(stream, overwrite: true);
                }

                var fileInfo = new FileInfo(localFilePath);
                _logger.LogInformation($"✓ Uploaded {fileName} ({fileInfo.Length / 1024 / 1024}MB)");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Upload failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Partitioning for Analytics
        // ============================================================================
        /// <summary>
        /// Organize data by date/category for efficient querying
        /// Benefits: Query only relevant data (skip rest)
        /// Example: SELECT * FROM table WHERE year=2024 AND month=1
        ///   → Queries only year=2024, month=1 partition
        ///   → Skips all other data
        /// </summary>
        public class PartitioningStrategy
        {
            public const string FOLDER_STRUCTURE = @"
                /data/raw/
                ├── year=2024/
                │   ├── month=01/
                │   │   ├── day=01/
                │   │   │   └── events.parquet
                │   │   ├── day=02/
                │   │   │   └── events.parquet
                │   │   └── day=03/
                │   │       └── events.parquet
                │   ├── month=02/
                │   │   ├── day=01/
                │   │   └── ...
                │   └── ...
                └── year=2025/
                    └── ...

                Why this structure?
                ✓ Pruning: Skip entire folders in WHERE clause
                ✓ Parallelism: Process each partition in parallel
                ✓ Versioning: Keep multiple snapshots of data

                Query example (Synapse):
                SELECT COUNT(*) FROM events
                WHERE year=2024 AND month=1
                → Only reads files in year=2024/month=01/
                → Ignores all other folders (fast!)";

            public const string PARTITIONING_BENEFITS = @"
                Data size: 100GB of events

                NO PARTITIONING:
                Query: 'SELECT * WHERE date = 2024-01-15'
                → Must scan entire 100GB
                → Takes 30 seconds

                WITH PARTITIONING (by year/month/day):
                Query: 'SELECT * WHERE year=2024 AND month=01 AND day=15'
                → Only scans 1GB (1/100th)
                → Takes 1 second
                → 30x faster!";
        }

        // ============================================================================
        // Pattern 4: List and Query Files
        // ============================================================================
        /// <summary>
        /// Enumerate files in directory
        /// Use case: Monitor incoming data, verify completeness
        /// </summary>
        public async Task<List<DataLakeFileInfo>> ListFilesAsync(
            string directoryPath,
            string filePattern = "*.parquet")
        {
            try
            {
                _logger.LogInformation($"→ Listing files in {directoryPath}");

                var directoryClient = _fileSystemClient.GetDirectoryClient(directoryPath);
                var files = new List<DataLakeFileInfo>();

                // List all files recursively
                await foreach (var item in directoryClient.GetPathsAsync(recursive: true))
                {
                    if (!item.IsDirectory && item.Name.EndsWith(filePattern.TrimStart('*')))
                    {
                        files.Add(new DataLakeFileInfo
                        {
                            Name = Path.GetFileName(item.Name),
                            Path = item.Name,
                            SizeBytes = item.ContentLength ?? 0,
                            LastModified = item.LastModified
                        });
                    }
                }

                _logger.LogInformation($"✓ Found {files.Count} files");
                foreach (var file in files)
                {
                    _logger.LogInformation($"  {file.Name} ({file.SizeBytes / 1024 / 1024}MB)");
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ File listing failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Data Retention and Lifecycle
        // ============================================================================
        /// <summary>
        /// Automatically delete/archive old data
        /// Cost optimization: Move old data to Archive tier ($0.0035/GB/month)
        /// </summary>
        public static class DataLifecycle
        {
            public const string LIFECYCLE_POLICY = @"
                Purpose: Balance cost and data availability

                POLICY:
                1. Fresh Data (< 30 days): Hot tier ($0.03/GB/month)
                   - Used for active analytics
                   - High performance

                2. Warm Data (30-90 days): Cool tier ($0.015/GB/month)
                   - Used occasionally
                   - Lower cost

                3. Cold Data (> 90 days): Archive tier ($0.0035/GB/month)
                   - Rarely accessed
                   - 95% cost savings vs hot
                   - Takes 1-15 hours to read (cold retrieval penalty)

                4. Very Old Data (> 1 year): Delete
                   - Archive elsewhere if compliance required
                   - Save storage costs

                Example: 1TB of data
                Hot 30 days: 1TB × 30 days × $0.03/GB = $900
                Cool 60 days: 1TB × 60 days × $0.015/GB = $900
                Archive 275 days: 1TB × 275 days × $0.0035/GB = $962
                Total: ~$2,700/year (vs $10,950 if all hot)";
        }

        // ============================================================================
        // Pattern 6: Data Formats (Parquet vs CSV)
        // ============================================================================
        public static class DataFormats
        {
            public const string COMPARISON = @"
                PARQUET (RECOMMENDED):
                Format: Columnar, compressed binary
                Size: 1GB CSV → 100MB Parquet (10x smaller!)
                Speed: Fast (columnar access)
                Cost: Lower (smaller storage)
                Use: Data lakes, analytics, Synapse
                Example: events.parquet

                CSV (LEGACY):
                Format: Text, comma-separated
                Size: Large (no compression)
                Speed: Slow (parse text)
                Cost: Higher (larger storage)
                Use: Export to users, legacy systems
                Example: events.csv

                JSON (NESTED):
                Format: Text, nested structure
                Size: Medium (some compression)
                Speed: Medium
                Cost: Medium
                Use: Semi-structured data, logs
                Example: events.json

                RECOMMENDATION:
                ✓ Store in Parquet (compressed, fast)
                ✓ Export to CSV only for users
                ✓ Use JSON for logs/events
                ✓ Saves 90% storage cost using Parquet";
        }

        // ============================================================================
        // Pattern 7: ADLS vs Blob Storage
        // ============================================================================
        public static class ADLSvsBlobStorage
        {
            public const string COMPARISON = @"
                AZURE DATA LAKE STORAGE (ADLS):
                Structure: Hierarchical (folders)
                Use: Analytics, big data, data warehousing
                Optimization: Directory traversal, partitioning
                Format: Parquet, ORC, Delta
                Best for: Petabytes of structured data
                Tools: Synapse, Databricks, Spark
                Cost: $0.03/GB/month

                AZURE BLOB STORAGE:
                Structure: Flat (just blobs)
                Use: Files, backups, media, documents
                Optimization: Parallel uploads, CDN
                Format: Any file type
                Best for: Website files, backups, archives
                Tools: Web apps, mobile, backup solutions
                Cost: $0.015/GB/month

                DECISION:
                Use ADLS if: Analytics, big data, structured data
                Use Blob if: Files, backups, websites, documents";
        }

        // ============================================================================
        // Pattern 8: Cost Optimization
        // ============================================================================
        public static class CostOptimization
        {
            public const string STRATEGIES = @"
                1. COMPRESSION
                   CSV 1TB → Parquet 100GB (90% savings)
                   Cost: $30/month vs $300/month

                2. LIFECYCLE POLICIES
                   Hot 30d → Cool 90d → Archive 1yr → Delete
                   Saves 70% on old data

                3. PARTITIONING
                   Query 1GB instead of 100GB
                   Same data, faster, cost-effective
                   Enables cheaper compute (less scan)

                4. DEDUPLICATION
                   Remove duplicate records
                   Reduces storage by 20-40%

                5. AGGREGATION
                   Store summaries, not raw events
                   Example: 1M events → 1K daily summaries
                   99% storage reduction

                Real Example:
                Current: 1PB raw data at $30/GB = $30M/month
                After optimization:
                - Compress to Parquet: 100TB ($3M)
                - Lifecycle tiers: $1.5M
                - Partition: Better queries ($1M compute)
                Total: $5.5M (82% savings!)";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class DataLakeFileInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long SizeBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}
