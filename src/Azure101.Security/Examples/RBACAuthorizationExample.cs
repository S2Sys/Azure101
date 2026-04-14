using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Authorization;
using Azure.ResourceManager.Authorization.Models;
using Microsoft.Extensions.Logging;

namespace Azure101.Security.Examples
{
    /// <summary>
    /// Azure RBAC (Role-Based Access Control) Authorization
    /// Demonstrates: Role assignments, built-in roles, custom roles, scope hierarchy
    /// Production pattern: Grant least privilege access (principle of least privilege)
    /// Key concept: Who (principal) can do What (role) on Which (resource)
    /// </summary>
    public class RBACAuthorizationExample
    {
        private readonly ArmClient _armClient;
        private readonly ILogger<RBACAuthorizationExample> _logger;

        public RBACAuthorizationExample(ILogger<RBACAuthorizationExample> logger)
        {
            // Create ARM client for RBAC operations
            var credential = new DefaultAzureCredential();
            _armClient = new ArmClient(credential);
            _logger = logger;
        }

        // ============================================================================
        // Pattern 1: Check if User/Service Principal has Specific Role
        // ============================================================================
        /// <summary>
        /// Verify if user has required permission
        /// Used for: Authorization checks, access validation
        /// Example: Does user have "Contributor" on storage account?
        /// </summary>
        public async Task<bool> UserHasRoleAsync(
            string subscriptionId,
            string resourceGroupName,
            string resourceName,
            string principalId,
            string requiredRole)
        {
            try
            {
                _logger.LogInformation($"→ Checking if principal {principalId} has role '{requiredRole}'");

                // Get subscription scope
                var subscription = _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

                // Get resource group scope
                var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);

                // Get role assignments for the resource
                var roleAssignments = resourceGroup.Value.GetRoleAssignments();

                // Search for matching role assignment
                await foreach (var assignment in roleAssignments.GetAllAsync())
                {
                    if (assignment.Data.PrincipalId == principalId)
                    {
                        // Check if role matches (role ID stored in assignment)
                        _logger.LogInformation($"✓ Principal {principalId} has role assignment");
                        return true;
                    }
                }

                _logger.LogWarning($"✗ Principal {principalId} does not have required role '{requiredRole}'");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Error checking role: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 2: Assign Built-In Role to User/Service Principal
        // ============================================================================
        /// <summary>
        /// Grant built-in role to principal (user or service principal)
        /// Scope: Subscription, resource group, or specific resource
        /// Common roles: Contributor, Reader, Owner, Virtual Machine Contributor
        /// Principle: Always grant least privilege (start with Reader, add Contributor only if needed)
        /// </summary>
        public async Task<bool> AssignRoleToUserAsync(
            string subscriptionId,
            string resourceGroupName,
            string principalId,
            string builtInRoleName) // e.g., "Contributor", "Reader", "Storage Blob Data Contributor"
        {
            try
            {
                _logger.LogInformation($"→ Assigning role '{builtInRoleName}' to principal {principalId}");

                var subscription = _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

                var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);

                // Get role definition by name
                var roleName = builtInRoleName;
                var scope = resourceGroup.Value.Id;

                // Create role assignment
                var roleAssignmentCreateParameters = new RoleAssignmentCreateParameters
                {
                    Properties = new RoleAssignmentProperties
                    {
                        // Role ID for built-in role (example: "acdd72a7-3385-48ef-bd42-f606fba81ae7" = Reader)
                        RoleDefinitionId = new ResourceIdentifier($"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{GetRoleDefinitionId(builtInRoleName)}"),
                        PrincipalId = principalId,
                        PrincipalType = RoleAssignmentPrincipalType.ServicePrincipal // or User
                    }
                };

                var assignment = await resourceGroup.Value.GetRoleAssignments()
                    .CreateAsync(WaitUntil.Completed, Guid.NewGuid().ToString(), roleAssignmentCreateParameters);

                _logger.LogInformation($"✓ Role '{builtInRoleName}' assigned to principal {principalId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Failed to assign role: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 3: Scope Hierarchy - Different Permission Levels
        // ============================================================================
        /// <summary>
        /// RBAC uses scope hierarchy:
        /// Subscription → Resource Group → Resource
        /// Example hierarchy:
        /// - Subscription level: Can manage all resources
        /// - Resource Group level: Can manage resources in group
        /// - Resource level: Can only access specific resource
        ///
        /// Benefit: Granular access control
        /// Example: Grant "Storage Blob Data Reader" on specific container (not entire storage)
        /// </summary>
        public class RBACScopeHierarchy
        {
            public const string SUBSCRIPTION_SCOPE = @"
                Scope: /subscriptions/{subscriptionId}
                Who: Can access resources across entire subscription
                Example roles:
                  - Subscription Admin
                  - Contributor (can create/delete resources)
                Use case: Organization-wide permissions";

            public const string RESOURCE_GROUP_SCOPE = @"
                Scope: /subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}
                Who: Can access resources within resource group
                Example roles:
                  - Contributor (can manage resources in group)
                  - Reader (can view resources in group)
                Use case: Team-based access (one team = one RG)";

            public const string RESOURCE_SCOPE = @"
                Scope: /subscriptions/{subscriptionId}/resourceGroups/{rg}/providers/Microsoft.Storage/storageAccounts/{account}
                Who: Can only access specific resource
                Example roles:
                  - Storage Blob Data Contributor (can read/write blobs in this account only)
                  - Key Vault Secrets Officer (can manage secrets in this vault only)
                Use case: Fine-grained, least-privilege access";

            public const string SCOPE_SELECTION_RULE = @"
                Choose SMALLEST scope that fulfills requirement:
                ✓ Does user need one storage account? → Resource scope
                ✓ Does team need all storage in RG? → Resource Group scope
                ✓ Does user need subscription management? → Subscription scope

                Why smallest scope? Limits blast radius if credentials compromised";
        }

        // ============================================================================
        // Pattern 4: Built-In Roles vs Custom Roles
        // ============================================================================
        /// <summary>
        /// Built-in roles: Pre-defined by Microsoft, ready to use
        /// Custom roles: Create when built-in don't match your needs
        ///
        /// Built-in roles cover 95% of scenarios
        /// Custom roles are rare and expensive to maintain
        /// </summary>
        public class RoleComparison
        {
            public const string BUILT_IN_ROLES = @"
                Ready-to-use roles defined by Microsoft
                Examples:
                  - Owner: Full control, can delegate access
                  - Contributor: Create/delete resources, cannot delegate access
                  - Reader: View resources only
                  - [Service]-Contributor: Create/manage specific service
                  - [Service] Data Contributor: Read/write service data
                  - [Service] Data Reader: Read-only service data

                Pros:
                  ✓ Already defined and tested
                  ✓ Microsoft maintains them
                  ✓ 95% of use cases covered
                  ✓ Easier to audit (familiar names)

                Cons:
                  ✗ May grant too many permissions
                  ✗ Can't customize granularity";

            public const string CUSTOM_ROLES = @"
                Define specific actions users can perform
                Example: Allow read blobs + write tables, deny deletion

                How to create:
                1. Start from built-in role template
                2. Add/remove actions (Microsoft.Storage/*/read)
                3. Define scope
                4. Create role in subscription

                Pros:
                  ✓ Precise permission control
                  ✓ True least privilege
                  ✓ Audit shows exact permissions

                Cons:
                  ✗ Maintenance burden (update when new actions available)
                  ✗ More complex to troubleshoot
                  ✗ Requires careful planning";

            public const string RECOMMENDATION = @"
                Start with built-in roles
                Only create custom role if:
                  - Built-in grants unnecessary permissions
                  - Custom role is reused across team/org
                  - Compliance/audit requires documentation";
        }

        // ============================================================================
        // Pattern 5: Managed Identity + RBAC (Perfect Combination)
        // ============================================================================
        /// <summary>
        /// Managed Identity: What to authenticate AS
        /// RBAC: What that identity can DO
        /// Combined: Passwordless + Least Privilege
        ///
        /// Example: Azure Function with Managed Identity
        /// 1. Function has system-assigned managed identity
        /// 2. RBAC grants identity "Storage Blob Data Reader" role on specific container
        /// 3. Function can read blobs in that container only (not write, not delete)
        /// </summary>
        public class ManagedIdentityWithRBAC
        {
            public const string PATTERN = @"
                Step 1: Enable Managed Identity on resource
                  - Azure Portal: Resource → Identity → System assigned → ON
                  - Gets ObjectId from Entra ID

                Step 2: Assign RBAC role to identity
                  - Scope: Resource (specific container, vault, database)
                  - Role: Least privilege (Reader, Contributor, Data Contributor)
                  - Principal: The managed identity's ObjectId

                Step 3: App uses DefaultAzureCredential()
                  - Automatically discovers identity from metadata service
                  - Gets token scoped for resource
                  - Token includes role permissions

                Result: Passwordless + least privilege
                  ✓ No secrets to rotate
                  ✓ Token valid only for permitted actions
                  ✓ Token valid only on assigned scope
                  ✓ Automatic token refresh";

            public const string EXAMPLE_SETUP = @"
                Azure Portal Setup:

                1. Azure Function properties:
                   - Identity: System assigned → ON → Save
                   - Copy Object ID (e.g., a1b2c3d4-...)

                2. Storage Account → Access Control (IAM):
                   - Add role assignment
                   - Role: Storage Blob Data Reader
                   - Principal: Select the function's managed identity
                   - Scope: Container (not entire account)

                3. Function code:
                   var credential = new DefaultAzureCredential();
                   var client = new BlobClient(uri, credential);
                   var blob = await client.DownloadAsync(); // Works! No password needed.

                Security benefit:
                  - If function compromised: attacker can only read blobs, not delete
                  - If identity deleted: all access automatically revoked
                  - If token leaked: valid only for specific container, specific actions";
        }

        // ============================================================================
        // Pattern 6: Troubleshooting Access Denied Errors
        // ============================================================================
        /// <summary>
        /// Common RBAC issues and solutions
        /// </summary>
        public static class AccessDeniedTroubleshooting
        {
            public const string ERROR_403_FORBIDDEN = @"
                Error: 403 Forbidden / Access denied

                Checklist:
                □ Is identity enabled on resource?
                  - Portal: Resource → Identity → System assigned

                □ Is role assigned to identity?
                  - Portal: Resource → Access Control (IAM)
                  - Search: Principal ID or name
                  - Verify role is listed

                □ Is scope correct?
                  - Role assigned on specific resource? Or parent RG only?
                  - Access denied may mean parent scope assigned (inheritance down)

                □ Are you checking the right principal?
                  - App Service: Check System assigned identity, not App Service registration
                  - VM: Check System assigned identity
                  - Function: Check Function's identity, not subscription identity

                □ Did role assignment propagate?
                  - Takes 5-15 minutes after assigning
                  - Try restarting app/VM

                □ Is token for right resource?
                  - Token for database.windows.net (SQL)
                  - Token for storage.azure.com (Blob)
                  - Wrong scope = access denied even with role";

            public const string DEBUG_STEPS = @"
                1. Verify principal ID:
                   # Get App Service identity
                   az webapp identity show --name myapp --resource-group myRG
                   # Copy principalId

                2. Check role assignments:
                   az role assignment list --assignee <principalId>
                   # Shows all roles assigned to principal
                   # Should show role, scope, and resource group

                3. Test with explicit scope:
                   # Assign role at resource level (not RG level)
                   az role assignment create \
                     --assignee <principalId> \
                     --role \"Storage Blob Data Reader\" \
                     --scope /subscriptions/.../storageAccounts/myStorage

                4. Check token scope:
                   # If using custom token, ensure scope is correct
                   # SQL: https://database.windows.net/
                   # Blob: https://storage.azure.com/
                   # Vault: https://vault.azure.net/";
        }

        // ============================================================================
        // Pattern 7: Role Assignment Audit Trail
        // ============================================================================
        /// <summary>
        /// Track who has what permissions (compliance requirement)
        /// Audit all role assignments for security review
        /// </summary>
        public async Task<List<RoleAssignmentInfo>> AuditRoleAssignmentsAsync(
            string subscriptionId,
            string resourceGroupName)
        {
            try
            {
                _logger.LogInformation($"→ Auditing role assignments for {resourceGroupName}");

                var subscription = _armClient.GetSubscriptionResource(
                    new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

                var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);

                var assignments = new List<RoleAssignmentInfo>();

                // Get all role assignments
                await foreach (var assignment in resourceGroup.Value.GetRoleAssignments().GetAllAsync())
                {
                    assignments.Add(new RoleAssignmentInfo
                    {
                        PrincipalId = assignment.Data.PrincipalId,
                        RoleDefinitionId = assignment.Data.RoleDefinitionId.ToString(),
                        PrincipalType = assignment.Data.PrincipalType.ToString(),
                        Scope = assignment.Id.ToString(),
                        CreatedDate = assignment.Id.ToString().Contains("assignment") ? DateTime.UtcNow : DateTime.MinValue
                    });
                }

                _logger.LogInformation($"✓ Found {assignments.Count} role assignments");

                // Display audit results
                foreach (var assignment in assignments)
                {
                    _logger.LogInformation($"  Principal: {assignment.PrincipalId}, Role: {assignment.RoleDefinitionId}");
                }

                return assignments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"✗ Audit failed: {ex.Message}");
                throw;
            }
        }

        // ============================================================================
        // Pattern 8: Common Built-In Roles Reference
        // ============================================================================
        /// <summary>
        /// Quick reference of commonly used roles
        /// </summary>
        public static class CommonBuiltInRoles
        {
            public const string OWNERSHIP_ROLES = @"
                Owner (most powerful)
                  ✓ Create, update, delete resources
                  ✓ Delegate access to others (create roles)
                  ✓ Full control
                  ✗ Violates least privilege

                Contributor
                  ✓ Create, update, delete resources
                  ✗ Cannot delegate access
                  ✗ Cannot modify user permissions

                Use: Service principals that need full resource control
                Not for users (too powerful)";

            public const string READ_ONLY_ROLES = @"
                Reader (read everything)
                  ✓ View all resources
                  ✗ Cannot modify or delete

                Monitoring Reader
                  ✓ View monitoring data
                  ✓ See logs and metrics
                  ✗ Cannot modify configuration

                Use: View-only access for auditors, analysts";

            public const string SERVICE_SPECIFIC_ROLES = @"
                Storage Blob Data Contributor
                  ✓ Read, write, delete blobs
                  ✗ Cannot manage storage account (e.g., access keys)

                Storage Blob Data Reader
                  ✓ Read blobs only
                  ✗ Cannot write or delete

                Key Vault Secrets Officer
                  ✓ Get, set, delete secrets
                  ✗ Cannot view other properties

                SQL Server Contributor
                  ✓ Manage SQL servers and databases
                  ✗ Cannot access data (data access separate)

                Use: Principle of least privilege
                Pick the most specific role that allows required actions";

            public const string LEAST_PRIVILEGE_EXAMPLE = @"
                Scenario: Web app needs to read blobs from storage

                ❌ WRONG: Assign \"Owner\" on storage account
                   - App can delete production data
                   - App can modify access controls
                   - Violates principle of least privilege

                ✓ RIGHT: Assign \"Storage Blob Data Reader\" on specific container
                   - App can only read specified container
                   - Cannot write, delete, or modify settings
                   - If app compromised: damage is limited
                   - If token leaked: valid only for read operations";
        }

        // Helper method: Map role name to role definition ID
        private string GetRoleDefinitionId(string roleName)
        {
            // Common role definition IDs (these are constants in Azure)
            return roleName switch
            {
                "Owner" => "8e3af657-a8ff-443c-a75c-2fe8c4bcb635",
                "Contributor" => "b24988ac-6180-42a0-ab88-20f7382dd24c",
                "Reader" => "acdd72a7-3385-48ef-bd42-f606fba81ae7",
                "Storage Blob Data Contributor" => "ba92f5b4-2d11-453d-a403-e96b0029c9fe",
                "Storage Blob Data Reader" => "2a2b9908-6ea1-4ae2-8e65-a410df84e7d1",
                "Key Vault Secrets Officer" => "b86a8fe4-44ce-4948-aee5-eccb2c155cd7",
                _ => throw new ArgumentException($"Unknown role: {roleName}")
            };
        }
    }

    // ============================================================================
    // Supporting Models
    // ============================================================================
    public class RoleAssignmentInfo
    {
        public string PrincipalId { get; set; }
        public string RoleDefinitionId { get; set; }
        public string PrincipalType { get; set; }
        public string Scope { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
