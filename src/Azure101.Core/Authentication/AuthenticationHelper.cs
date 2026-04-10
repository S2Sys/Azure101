using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace Azure101.Core.Authentication;

/// <summary>
/// Helper class for establishing authentication to Azure services
/// Supports multiple authentication methods: DefaultAzureCredential, Connection String, Managed Identity
/// </summary>
public class AuthenticationHelper
{
    private readonly ILogger<AuthenticationHelper> _logger;

    public AuthenticationHelper(ILogger<AuthenticationHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a DefaultAzureCredential which tries multiple authentication methods in order
    /// - EnvironmentCredential
    /// - ManagedIdentityCredential
    /// - SharedTokenCacheCredential
    /// - VisualStudioCredential
    /// - VisualStudioCodeCredential
    /// - AzureCliCredential
    /// - PowerShellCredential
    /// </summary>
    public TokenCredential GetDefaultAzureCredential()
    {
        _logger.LogInformation("Creating DefaultAzureCredential");
        return new DefaultAzureCredential();
    }

    /// <summary>
    /// Creates credentials from environment variables
    /// Requires: AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET
    /// </summary>
    public TokenCredential GetClientSecretCredential()
    {
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
            ?? throw new InvalidOperationException("AZURE_TENANT_ID environment variable not found");
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
            ?? throw new InvalidOperationException("AZURE_CLIENT_ID environment variable not found");
        var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")
            ?? throw new InvalidOperationException("AZURE_CLIENT_SECRET environment variable not found");

        _logger.LogInformation("Creating ClientSecretCredential for tenant {TenantId}", tenantId);
        return new ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    /// <summary>
    /// Creates managed identity credentials
    /// Uses the system-assigned or user-assigned managed identity
    /// </summary>
    public TokenCredential GetManagedIdentityCredential(string? clientId = null)
    {
        _logger.LogInformation("Creating ManagedIdentityCredential" +
            (clientId != null ? $" with clientId {clientId}" : ""));
        return new ManagedIdentityCredential(clientId);
    }

    /// <summary>
    /// Validates that the provided credential can authenticate
    /// </summary>
    public async Task ValidateCredentialAsync(TokenCredential credential)
    {
        try
        {
            var token = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                CancellationToken.None);
            _logger.LogInformation("Credential validation successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credential validation failed");
            throw;
        }
    }
}
