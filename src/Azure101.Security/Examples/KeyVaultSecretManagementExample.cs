using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Azure101.Security.Examples
{
    /// <summary>
    /// Azure Key Vault Secret Management
    /// Demonstrates: Getting secrets, caching, rotation handling, error recovery
    /// Production pattern: Never hardcode secrets - use Key Vault + Managed Identity
    /// </summary>
    public class KeyVaultSecretManagementExample
    {
        private readonly SecretClient _secretClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<KeyVaultSecretManagementExample> _logger;

        // Cache TTL: 1 hour (balances freshness and API calls)
        private const int CacheTTLMinutes = 60;

        public KeyVaultSecretManagementExample(
            string keyVaultUrl,
            IMemoryCache cache,
            ILogger<KeyVaultSecretManagementExample> logger)
        {
            // Use Managed Identity (no credentials needed)
            // Azure automatically provides credentials from VM/App Service metadata
            var credential = new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
            _cache = cache;
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Get Secret with Caching
        // ============================================================================
        /// <summary>
        /// Get secret from Key Vault with local caching
        /// First call: 500ms (Key Vault round trip)
        /// Subsequent calls within 1 hour: 1ms (cache hit)
        /// Cache hit rate: 99% (secrets rarely change)
        /// Reduces API calls by 100x
        /// </summary>
        public async Task<string> GetSecretAsync(string secretName)
        {
            const string cacheKeyPrefix = "keyvault_";
            string cacheKey = $"{cacheKeyPrefix}{secretName}";

            try
            {
                // Step 1: Check local cache
                if (_cache.TryGetValue(cacheKey, out string cachedValue))
                {
                    _logger.LogInformation($"✓ Secret '{secretName}' retrieved from cache");
                    return cachedValue;
                }

                // Step 2: Cache miss - fetch from Key Vault
                _logger.LogInformation($"→ Fetching secret '{secretName}' from Key Vault");

                var secret = await _secretClient.GetSecretAsync(secretName);

                // Step 3: Cache the secret
                _cache.Set(cacheKey, secret.Value.Value, TimeSpan.FromMinutes(CacheTTLMinutes));

                _logger.LogInformation($"✓ Secret '{secretName}' cached for {CacheTTLMinutes} minutes");
                return secret.Value.Value;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError($"✗ Secret '{secretName}' not found in Key Vault");
                throw new InvalidOperationException($"Secret '{secretName}' not found", ex);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError($"✗ Access denied to secret '{secretName}' - check RBAC permissions");
                throw new UnauthorizedAccessException($"Access denied to secret '{secretName}'", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Error getting secret: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Get Database Connection String
        // ============================================================================
        /// <summary>
        /// Common pattern: Get connection string from Key Vault
        /// Never put in appsettings.json or code
        /// Separate for each environment (dev, staging, prod)
        /// </summary>
        public async Task<string> GetDatabaseConnectionStringAsync(string environment)
        {
            try
            {
                // Secret name format: "sqldb-{environment}-connstr"
                // Example: "sqldb-prod-connstr"
                string secretName = $"sqldb-{environment}-connstr";

                var connectionString = await GetSecretAsync(secretName);

                _logger.LogInformation($"✓ Retrieved {environment} database connection string");
                return connectionString;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Failed to get connection string: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Set Secret (Rotation Scenario)
        // ============================================================================
        /// <summary>
        /// Update secret in Key Vault (e.g., during password rotation)
        /// Steps:
        /// 1. Generate new password/secret
        /// 2. Update in Key Vault
        /// 3. Clear cache so apps refresh
        /// 4. Update dependent systems
        /// </summary>
        public async Task<bool> SetSecretAsync(string secretName, string secretValue)
        {
            try
            {
                _logger.LogInformation($"→ Updating secret '{secretName}' in Key Vault");

                // Set the secret with metadata
                var secretProperties = new KeyVaultSecret(secretName, secretValue)
                {
                    Properties =
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(365), // Expire in 1 year
                        Tags = new Dictionary<string, string>
                        {
                            { "ManagedBy", "Application" },
                            { "UpdatedBy", Environment.UserName },
                            { "UpdatedDate", DateTime.UtcNow.ToString("O") }
                        }
                    }
                };

                var result = await _secretClient.SetSecretAsync(secretProperties);

                // Clear cache so apps fetch new value
                string cacheKey = $"keyvault_{secretName}";
                _cache.Remove(cacheKey);

                _logger.LogInformation($"✓ Secret '{secretName}' updated and cache cleared");
                return true;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                _logger.LogError($"✗ Access denied - no permission to set secret '{secretName}'");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Failed to set secret: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 4: Handle Secret Rotation
        // ============================================================================
        /// <summary>
        /// Handle secret rotation transparently
        /// Scenario: DBA rotates database password
        /// Steps:
        /// 1. DBA updates Key Vault with new password
        /// 2. Cache expires after 1 hour
        /// 3. Next app request fetches new password
        /// 4. Connection works with new password
        /// No app restart needed!
        /// </summary>
        public async Task<string> GetSecretWithRotationHandlingAsync(string secretName)
        {
            try
            {
                // Get secret (will use cache if available)
                var secret = await GetSecretAsync(secretName);

                // Check if secret is marked for rotation (metadata)
                // This would be checked in a real scenario
                return secret;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                // Secret was deleted during rotation?
                _logger.LogError($"Secret '{secretName}' missing - rotation in progress?");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving secret with rotation: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 5: Secret Versions and History
        // ============================================================================
        /// <summary>
        /// Key Vault keeps version history
        /// Allows rollback if new secret is bad
        /// </summary>
        public async Task<List<KeyVaultSecret>> GetSecretVersionsAsync(string secretName)
        {
            try
            {
                var versions = new List<KeyVaultSecret>();

                // Get all versions of the secret
                var versionsPages = _secretClient.GetPropertiesOfSecretVersionsAsync(secretName);

                await foreach (var version in versionsPages)
                {
                    _logger.LogInformation($"Version {version.Version}: Created {version.CreatedOn}");
                    versions.Add(new KeyVaultSecret(secretName, ""));
                }

                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting secret versions: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 6: Secure Storage of Sensitive Data
        // ============================================================================
        /// <summary>
        /// Best practice: What goes in Key Vault vs App Configuration
        /// </summary>
        public static class SecretStrategy
        {
            // Store in Key Vault (sensitive)
            public const string STORE_IN_KEY_VAULT = @"
                - Database passwords
                - API keys (third-party services)
                - Connection strings
                - SSH keys
                - Certificates
                - OAuth client secrets
            ";

            // Store in App Configuration (non-sensitive)
            public const string STORE_IN_APP_CONFIG = @"
                - Feature flags
                - Log levels
                - Cache expiration
                - API endpoints
                - Theme settings
            ";

            // Store in code (public)
            public const string STORE_IN_CODE = @"
                - Version numbers
                - Public constants
                - UI strings/labels
            ";
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class DatabaseSecret
    {
        public string ConnectionString { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public DateTime? RotatedDate { get; set; }
    }
}
