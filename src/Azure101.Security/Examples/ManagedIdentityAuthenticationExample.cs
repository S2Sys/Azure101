using System;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azure101.Security.Examples
{
    /// <summary>
    /// Azure Managed Identity - Passwordless Authentication
    /// Demonstrates: System-assigned identity, getting tokens, accessing Azure resources
    /// Production pattern: Use Managed Identity instead of credentials
    /// Benefits: No credential rotation, no exposure, automatic renewal
    /// </summary>
    public class ManagedIdentityAuthenticationExample
    {
        private readonly ILogger<ManagedIdentityAuthenticationExample> _logger;

        public ManagedIdentityAuthenticationExample(ILogger<ManagedIdentityAuthenticationExample> logger)
        {
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Implicit Token from Metadata Service
        // ============================================================================
        /// <summary>
        /// DefaultAzureCredential automatically gets token from VM/App Service
        /// No credentials needed - Azure provides them implicitly
        /// Process:
        /// 1. App Service/VM has system-assigned identity
        /// 2. Azure metadata service provides token automatically
        /// 3. Token is valid for 1 hour
        /// 4. SDK auto-refreshes when expired
        /// </summary>
        public async Task<string> GetTokenForSqlDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("→ Getting token for SQL Database from Managed Identity");

                // DefaultAzureCredential checks multiple sources:
                // 1. Environment variables (for local dev)
                // 2. Managed Identity (for App Service/VM)
                // 3. Cached credentials
                var credential = new DefaultAzureCredential();

                // Get token scoped for SQL Database
                // Scope: https://database.windows.net/
                var tokenRequest = new TokenRequestContext(
                    new[] { "https://database.windows.net/.default" });

                var token = await credential.GetTokenAsync(tokenRequest);

                _logger.LogInformation($"✓ Token acquired (expires in {token.ExpiresOn.Subtract(DateTime.UtcNow).TotalMinutes} minutes)");
                return token.Token;
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogError($"✗ Authentication failed: {ex.Message}");
                _logger.LogError("→ Is Managed Identity configured on VM/App Service?");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Error getting token: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Access SQL Database with Managed Identity
        // ============================================================================
        /// <summary>
        /// Connect to SQL Database using Managed Identity token
        /// No password hardcoded - token is temporary and auto-refreshed
        /// Steps:
        /// 1. Get token for SQL Database scope
        /// 2. Set token as password
        /// 3. Connect with token (valid 1 hour)
        /// 4. On next connection, new token obtained
        /// </summary>
        public async Task<int> QueryDatabaseWithManagedIdentityAsync(
            string sqlServer,
            string database)
        {
            try
            {
                _logger.LogInformation($"→ Connecting to {sqlServer}/{database} with Managed Identity");

                // Step 1: Get token
                var credential = new DefaultAzureCredential();
                var tokenRequest = new TokenRequestContext(
                    new[] { "https://database.windows.net/.default" });

                var token = await credential.GetTokenAsync(tokenRequest);

                // Step 2: Build connection string (no password!)
                var connectionString = $"Server=tcp:{sqlServer},1433;Initial Catalog={database};Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;";

                // Step 3: Connect using token as password
                await using (var connection = new SqlConnection(connectionString))
                {
                    // Set token as password
                    connection.AccessToken = token.Token;

                    // Open connection with token
                    await connection.OpenAsync();

                    _logger.LogInformation("✓ Connected to SQL Database with Managed Identity");

                    // Execute query
                    const string query = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES";

                    await using (var command = new SqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        _logger.LogInformation($"✓ Query result: {result} tables in database");
                        return (int)result;
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 18456)
            {
                _logger.LogError("✗ SQL authentication failed - check RBAC permissions");
                _logger.LogError("→ Verify: ALTER SERVER ROLE ##MS_DatabaseAuthenticator## ADD MEMBER [service-principal-name]");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Database error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Access Blob Storage with Managed Identity
        // ============================================================================
        /// <summary>
        /// Access Blob Storage using Managed Identity
        /// No connection string with key - just storage account URL
        /// Azure handles authentication automatically
        /// </summary>
        public async Task<bool> UploadBlobWithManagedIdentityAsync(
            string storageAccountUrl,
            string containerName,
            string blobName,
            byte[] data)
        {
            try
            {
                _logger.LogInformation($"→ Uploading blob to {storageAccountUrl}/{containerName}/{blobName}");

                // Create client with Managed Identity (no key needed!)
                var credential = new DefaultAzureCredential();
                var blobContainerClient = new BlobContainerClient(
                    new Uri($"{storageAccountUrl}/{containerName}"),
                    credential);

                // Upload blob
                var blobClient = blobContainerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(
                    BinaryData.FromBytes(data),
                    overwrite: true);

                _logger.LogInformation($"✓ Blob uploaded successfully");
                return true;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError($"✗ Access denied to Blob Storage - check RBAC permissions");
                _logger.LogError("→ Required role: Storage Blob Data Contributor");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Upload error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Access Table Storage with Managed Identity
        // ============================================================================
        /// <summary>
        /// Access Table Storage using Managed Identity
        /// Create/update/delete tables without connection string keys
        /// </summary>
        public async Task<bool> CreateTableWithManagedIdentityAsync(
            string storageAccountUrl,
            string tableName)
        {
            try
            {
                _logger.LogInformation($"→ Creating table '{tableName}' in {storageAccountUrl}");

                // Create client with Managed Identity
                var credential = new DefaultAzureCredential();
                var tableServiceClient = new TableServiceClient(
                    new Uri(storageAccountUrl),
                    credential);

                // Create table
                await tableServiceClient.CreateTableIfNotExistsAsync(tableName);

                _logger.LogInformation($"✓ Table '{tableName}' created");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Table creation error: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Token Caching and Refresh
        // ============================================================================
        /// <summary>
        /// Azure SDK automatically handles token caching and refresh
        /// First call: Get token from metadata service (~100ms)
        /// Subsequent calls: Use cached token (< 1ms)
        /// On expiration: Auto-refresh transparently
        /// </summary>
        public async Task DemonstrateTokenCachingAsync()
        {
            var credential = new DefaultAzureCredential();

            _logger.LogInformation("→ Demonstrating token caching");

            try
            {
                // First call - fetch from metadata service
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var tokenRequest = new TokenRequestContext(
                    new[] { "https://database.windows.net/.default" });

                var token1 = await credential.GetTokenAsync(tokenRequest);
                sw.Stop();

                _logger.LogInformation($"✓ First call (from metadata service): {sw.ElapsedMilliseconds}ms");

                // Second call - use cached token
                sw.Restart();
                var token2 = await credential.GetTokenAsync(tokenRequest);
                sw.Stop();

                _logger.LogInformation($"✓ Second call (from cache): {sw.ElapsedMilliseconds}ms");

                // Verify same token
                if (token1.Token == token2.Token)
                {
                    _logger.LogInformation("✓ Token cached successfully (same token both calls)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Caching demo error: {ex.Message}");
            }
        }

        // ============================================================================
        // Pattern 6: System-Assigned vs User-Assigned Identity
        // ============================================================================
        /// <summary>
        /// Comparison of identity types
        /// </summary>
        public static class IdentityComparison
        {
            // System-Assigned Identity (tied to single resource)
            public const string SYSTEM_ASSIGNED = @"
                ✓ Automatic creation with resource
                ✓ No additional cost
                ✓ Deleted with resource
                ✗ Can't share between resources

                Use for: Single resource access
            ";

            // User-Assigned Identity (reusable)
            public const string USER_ASSIGNED = @"
                ✓ Create once, use multiple times
                ✓ Persists if resource deleted
                ✓ Share between resources
                ✗ Requires explicit assignment

                Use for: Multiple resources, cross-resource access
            ";

            public const string SELECTION_RULE = @"
                Start with System-Assigned (simpler)
                Move to User-Assigned if need sharing
            ";
        }

        // ============================================================================
        // Pattern 7: Error Handling and Troubleshooting
        // ============================================================================
        /// <summary>
        /// Common errors and solutions
        /// </summary>
        public static class TroubleshootingGuide
        {
            public const string ERROR_NO_IDENTITY = @"
                Error: No credentials were found
                Solution:
                - Verify VM/App Service has Managed Identity enabled
                - Check Azure Portal: Settings → Identity → System assigned → ON
                - Restart app/VM after enabling
            ";

            public const string ERROR_PERMISSION_DENIED = @"
                Error: Access denied (403)
                Solution:
                - Verify identity has RBAC role assigned
                - Common roles:
                  * Storage Blob Data Contributor
                  * SQL Server Contributor
                  * Key Vault Secrets Officer
                - Check: Azure Portal → Resource → Access Control → Add role
            ";

            public const string ERROR_TOKEN_EXPIRED = @"
                Error: Token expired
                Solution:
                - SDK auto-refreshes tokens
                - If manual handling, request new token
                - Check system time sync (important!)
            ";
        }
    }

    // ============================================================================
    // Configuration Helper
    // ============================================================================
    public static class ManagedIdentityConfiguration
    {
        /// <summary>
        /// How to enable Managed Identity in code
        /// In Startup.cs or Program.cs:
        /// </summary>
        public static string GetStartupCode => @"
            // In Startup.cs ConfigureServices():

            // Option 1: System-Assigned Identity (default)
            services.AddScoped(_ => new DefaultAzureCredential());

            // Option 2: User-Assigned Identity (specific identity)
            var credential = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = ""<client-id>""
                });
            services.AddScoped(_ => credential);
        ";
    }
}
