# Azure Security Services - Complete Wiki

## Overview

Azure Security provides identity management, secret storage, access control, and compliance monitoring. Zero-trust security model with focus on authentication, authorization, and encryption.

---

## Quick Comparison Table

| Service | Purpose | Type | Cost | Use Case |
|---------|---------|------|------|----------|
| **Key Vault** | Secrets storage | Secrets | Per-secret | API keys, passwords, certificates |
| **App Configuration** | Configuration mgmt | Config | Per-day | Feature flags, app settings |
| **Managed Identity** | Eliminate credentials | Auth | Free | Service-to-service auth |
| **Azure AD** | Identity & Access | IAM | Per-user/month | User authentication, authorization |
| **RBAC** | Role-based access | Authorization | Free | Fine-grained permissions |
| **App Protection** | DDoS/WAF | Security | Variable | Web application protection |

---

## 1. Azure Key Vault

### What is it?
Managed service for securely storing and accessing secrets, keys, and certificates without exposing them in code or logs.

### When to Use
- Store database passwords
- API keys and connection strings
- SSL/TLS certificates
- Encryption keys
- Manage certificate lifecycle
- Audit access to secrets

### Key Features
- **Secrets**: String values (passwords, API keys)
- **Keys**: Cryptographic keys for encryption/decryption
- **Certificates**: SSL/TLS and code signing certificates
- **Hardware Security Module (HSM)**: FIPS 140-2 Level 3
- **Soft Delete**: Recover accidentally deleted items
- **Purge Protection**: Prevent accidental permanent deletion
- **Access Policies**: Fine-grained permissions
- **Audit Logging**: Track who accessed what and when

### Architecture & Core Concepts

#### Secrets Management Lifecycle
```
1. Create Secret in Key Vault
   └─ Sensitive data stored securely (encrypted at rest)

2. Application needs secret
   ├─ Uses Managed Identity
   └─ Key Vault grants access via RBAC

3. Access is audited
   ├─ Logged with timestamp
   ├─ User identity
   └─ Success/failure

4. Rotate secret
   ├─ Schedule rotation every 90 days
   └─ Update all systems automatically
```

#### Authentication Methods
```
Direct (credentials-based)
    ├─ Username/password
    └─ ❌ Not recommended (exposed in code)

Managed Identity (recommended)
    ├─ Azure VM/App Service gets implicit token
    └─ ✅ No credentials needed, automatic rotation

Service Principal
    ├─ Application-specific identity
    └─ Use for non-Azure systems
```

### Pros & Cons

**Pros** ✅
- Centralized secret management
- Audit trail of all access
- Automatic rotation capability
- HSM option for compliance
- Integration with other Azure services
- Soft delete and purge protection
- Access policies per secret

**Cons** ❌
- Additional latency on first access (cache recommended)
- Extra cost per secret stored
- Requires Managed Identity setup
- Complexity for simple deployments
- Not for session tokens (use Redis instead)

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Vaults per subscription | 200 |
| Secrets per vault | Unlimited |
| Key size | 2-4KB |
| Certificate size | Unlimited |
| Access policies per vault | 1024 |
| Storage per vault | 2GB per secret |
| Throughput | 2000 requests/10 seconds |

### Real-World Use Cases

#### Use Case 1: Database Connection Strings
```
Web App (doesn't have password hardcoded)
├─ Calls Key Vault with Managed Identity
├─ Key Vault returns SQL connection string
├─ App connects to database
└─ Access is audited

Rotation:
├─ DBA updates password in SQL
├─ Updates secret in Key Vault
├─ App automatically uses new password
    (When cache expires)
```

#### Use Case 2: Multi-Environment Configuration
```
Development
├─ Key Vault: dev-vault
└─ Secrets: dev-api-key, dev-db-password

Staging
├─ Key Vault: staging-vault
└─ Secrets: staging-api-key, staging-db-password

Production
├─ Key Vault: prod-vault
└─ Secrets: prod-api-key, prod-db-password

Result: Same code, different secrets per environment
```

#### Use Case 3: Certificate Management
```
SSL/TLS Certificates
├─ Store in Key Vault
├─ Scheduled rotation (renewal before expiry)
├─ App Service auto-imports new cert
└─ Zero downtime certificate update

Compliance:
├─ Audit trail of certificate usage
├─ Who accessed certificate and when
└─ Meet regulatory requirements
```

### Performance Tips
- Cache secrets in memory (avoid constant vault calls)
- Use Managed Identity (no token exchange overhead)
- Implement connection pooling
- Monitor API rate limits
- Batch secrets in single vault

### Cost Optimization
- Consolidate secrets (fewer = lower cost)
- Batch operations
- Archive old secrets
- Monitor actual access patterns

---

## 2. Azure App Configuration

### What is it?
Managed service for centralizing application configuration and feature flags without code redeploy.

### When to Use
- Feature flags (enable/disable features)
- Environment-specific settings
- Configuration without redeployment
- A/B testing
- Blue-green deployments
- Gradual rollout of features

### Key Features
- **Key-Value Store**: Simple config storage
- **Feature Flags**: Enable/disable features dynamically
- **Snapshots**: Point-in-time configuration backup
- **Import/Export**: Bulk config management
- **Encryption**: Values encrypted in transit/at rest
- **Access Control**: RBAC for who can modify

### Pros & Cons

**Pros** ✅
- Dynamic config without redeployment
- Feature flags for safe rollouts
- A/B testing capability
- Access control per key
- Good price (per-day)
- Works with applications not on Azure

**Cons** ❌
- Not for sensitive data (use Key Vault)
- Requires app code changes
- API calls latency
- Size limits per config item

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Config items per store | 10,000 (standard), 100,000 (premium) |
| Value size | 10KB |
| Feature flags per store | Unlimited |
| Requests per second | Varies by tier |

### Real-World Use Cases

#### Use Case 1: Feature Toggle
```
New Feature (incomplete)
├─ Code deployed with feature flag = false
├─ Users don't see new feature

Ready for release:
├─ DBA/DevOps sets flag = true
├─ Some users see feature (gradual rollout)
└─ No code redeployment needed

Problem found:
├─ Set flag = false
└─ Feature hidden immediately
```

#### Use Case 2: Database Connection Pooling Size
```
App Configuration
├─ db-connection-pool-size = 20

Monitor (daily):
├─ Check database connection utilization
└─ Database CPU < 50%

If Database CPU > 80%:
├─ Update config: db-connection-pool-size = 10
└─ Reduce connection pool size
    (App reads new value next time)
```

---

## 3. Azure Managed Identity

### What is it?
Service identity for Azure resources (VMs, App Service, Functions) to authenticate with other Azure services without storing credentials.

### When to Use
- App Service accessing database
- VM accessing Key Vault
- Function accessing blob storage
- Service-to-service authentication
- No credential management needed

### Key Features
- **System-Assigned**: One identity per resource
- **User-Assigned**: Shared identity across resources
- **Automatic Token**: Azure manages authentication tokens
- **No Secrets**: No credentials to manage or rotate
- **RBAC Integration**: Assign permissions like normal users

### Architecture & Core Concepts

#### Types of Managed Identity
```
System-Assigned (per resource)
    ├─ VM 1 → Unique identity tied to VM 1
    └─ If VM deleted, identity deleted

User-Assigned (reusable)
    ├─ Create once
    ├─ Assign to multiple VMs/App Services
    └─ Survives resource deletion
    
Typical choice:
    ├─ Simple: System-Assigned
    └─ Complex: User-Assigned for sharing
```

#### Authentication Flow
```
1. App Service created with System-Managed Identity
2. App requests token from Azure token service
   └─ No credentials needed (implicit from VM metadata)
3. Azure token service verifies identity
4. App gets OAuth token (valid 1 hour)
5. App uses token to access resources
   └─ Key Vault, Storage, SQL Database
```

### Pros & Cons

**Pros** ✅
- No credentials to manage
- Automatic token refresh
- Audit trail built-in
- No code changes needed (with SDK)
- Highly secure (no credential exposure)

**Cons** ❌
- Only works for Azure services
- External systems need Service Principal instead
- Requires SDK/library support
- Slightly more latency than raw credentials

### Real-World Use Cases

#### Use Case 1: App Service to SQL Database
```
App Service with System-Managed Identity
    ├─ Gets implicit token
    └─ Uses token to connect to SQL Database

SQL Database RBAC:
    ├─ Grant "db_datareader" to App Service identity
    └─ App Service can read (but not write)

Result:
    ├─ No password stored in app
    ├─ No connection string with credentials
    └─ Fully audited access
```

#### Use Case 2: Azure Function to Key Vault
```
Function with Managed Identity
    ├─ Gets OAuth token implicitly
    └─ Calls Key Vault: "Get secret"

Key Vault RBAC:
    ├─ Grant "Get" permission to Function identity
    └─ Function can read specific secrets

Audit Log:
    ├─ When Function accessed secret
    ├─ What secret was accessed
    └─ Success/failure
```

---

## 4. Azure Active Directory (Azure AD / Entra ID)

### What is it?
Cloud-based identity and access management service. Manages who can access what resources.

### When to Use
- Authenticate users
- Authorize access to apps
- Sync on-premises users to cloud
- Multi-factor authentication (MFA)
- Conditional access policies
- B2B guest access

### Key Features
- **User Management**: Create and manage user accounts
- **Groups**: Organize users for bulk permission assignment
- **Apps**: Register applications
- **Conditional Access**: Require MFA for certain conditions
- **Multi-Factor Authentication**: SMS, authenticator app, FIDO2
- **Self-Service Password Reset**: Users reset own passwords

### Pros & Cons

**Pros** ✅
- Centralized identity management
- Multi-factor authentication
- Conditional access policies
- Integration with 3000+ SaaS apps
- Cloud and on-premises sync
- Strong security standards

**Cons** ❌
- Learning curve (complex system)
- Cost per user/month
- Requires planning for large deployments
- On-premises sync complexity
- Password policy enforcement challenging

### Real-World Use Cases

#### Use Case 1: Enterprise Application Access
```
Employee Email: john.doe@company.com
├─ Access to Office 365
├─ Access to internal web apps
├─ Access to cloud databases
└─ All controlled by Azure AD

Onboarding:
├─ Create user in Azure AD
├─ Assign to groups
└─ Permissions inherited from groups

Leaving:
├─ Disable user in Azure AD
└─ All access immediately revoked
```

#### Use Case 2: Conditional Access
```
Policy: Require MFA for external locations

User in office network:
    └─ Access granted (no MFA needed)

User accessing from coffee shop:
    └─ Require MFA
    └─ Must provide second factor (phone)

User accessing from high-risk location:
    └─ Block access entirely
    └─ Require admin approval
```

---

## 5. Role-Based Access Control (RBAC)

### What is it?
Fine-grained permission model. Grant "roles" (predefined permission sets) to users/groups at resource or scope level.

### When to Use
- Control who can do what
- Principle of least privilege
- Audit who has what permissions
- Separate duties (developer vs admin)
- Resource-level access control

### Key Features
- **Built-in Roles**: Owner, Contributor, Reader, + 200+ custom
- **Scope Levels**: Subscription, Resource Group, Resource
- **Assignable**: Users, Groups, Service Principals
- **Audit**: See who has what permissions
- **Time-Bound**: Just-in-time (JIT) access

### Pros & Cons

**Pros** ✅
- Simple permission model
- Reusable roles
- Audit trail built-in
- Scope flexibility
- Works with Managed Identity

**Cons** ❌
- Role combinations can be complex
- Hard to revoke specific permissions
- No fine-grained resource-level control
- Requires planning for large orgs

### Real-World Use Cases

#### Use Case 1: Developer vs DBA Separation
```
Developers
├─ Role: "Contributor" on Dev Resource Group
├─ Can create/delete resources in dev
└─ Cannot touch production

DBAs
├─ Role: "SQL Server Administrator" on Prod databases
├─ Can modify schema, manage backups
└─ Cannot modify networking

DevOps
├─ Role: "Owner" on Azure Pipelines
├─ Can manage CI/CD
└─ Cannot modify team members (separate process)
```

#### Use Case 2: Just-In-Time Access
```
Incident: Production database down

Regular: DBA has "Reader" on prod databases

Emergency: DBA requests JIT access
├─ System grants "Contributor" for 2 hours
├─ DBA can make emergency fixes
├─ Permission auto-revokes after 2 hours

Audit: Every action logged with timestamp
```

---

## 6. Web Application Firewall (WAF)

### What is it?
Layer 7 protection against common web attacks (SQL injection, XSS, DDoS).

### When to Use
- Web applications facing internet
- Protect against OWASP Top 10
- PCI-DSS compliance required
- High-traffic applications
- Prevent data exfiltration

### Key Features
- **Rule Sets**: Pre-configured protection rules
- **Custom Rules**: Create specific rules
- **Rate Limiting**: Prevent brute force attacks
- **IP Reputation**: Block known malicious IPs
- **Geo-Blocking**: Block specific countries
- **DDoS Protection**: Mitigate large attacks

### Real-World Use Cases

#### Use Case 1: E-Commerce Website Protection
```
External attackers
    ├─ SQL injection attempts → WAF blocks
    ├─ XSS attacks → WAF blocks
    ├─ DDoS attacks → WAF rate limits
    ├─ Brute force login → WAF blocks
    └─ Bot traffic → WAF blocks

Clean traffic
    └─ Reaches application normally
```

---

## Architecture Patterns

### Pattern 1: Zero-Trust Security
```
1. Verify Identity
   ├─ Azure AD authentication
   └─ Multi-factor authentication

2. Verify Device
   ├─ Must be enrolled/compliant
   └─ Check device security posture

3. Verify Network
   ├─ Private endpoint (no internet)
   └─ VPN or ExpressRoute

4. Grant Minimal Access
   ├─ RBAC with least privilege
   └─ Just-in-time access

Result: Never trust, always verify
```

### Pattern 2: Secret Management
```
Application (no hardcoded secrets)
    └─ Managed Identity (automatic auth)
    └─ Key Vault (get secrets)
    ├─ Database password
    ├─ API keys
    └─ SSL certificates

Rotation:
    ├─ Admin updates secret in Key Vault
    └─ Application auto-refreshes (cache TTL)
```

### Pattern 3: Network Security Layers
```
Layer 1: Internet
    └─ WAF (block attacks)

Layer 2: Network
    └─ NSGs (firewall rules)

Layer 3: Application
    └─ Authentication (Azure AD)

Layer 4: Data
    └─ Key Vault (encrypt secrets)
    └─ Database encryption

Result: Defense in depth
```

---

## Interview Questions

1. **Design secure multi-tier application**
   - Authentication: Azure AD for users
   - Authorization: RBAC for permissions
   - Secrets: Key Vault for connection strings
   - Network: VNets + NSGs + WAF
   - Audit: Log everything to Log Analytics

2. **Migrate on-premises users to Azure AD**
   - Install Azure AD Connect on-premises
   - Sync users to cloud
   - Implement hybrid scenario
   - Test before full migration
   - Plan for password sync vs pass-through auth

3. **Implement just-in-time access**
   - Use Azure AD Privileged Identity Management
   - Require justification for privileged access
   - Time-bound (auto-revoke)
   - Full audit trail
   - Approval workflow

4. **Secure API keys and connection strings**
   - Store in Key Vault (never in code)
   - Use Managed Identity to access
   - Rotate secrets every 90 days
   - Audit access logs
   - Delete unused secrets

5. **Zero-trust architecture**
   - Verify every access (don't trust network)
   - Authenticate (Azure AD)
   - Authorize (RBAC)
   - Encrypt everything (in transit + at rest)
   - Log and monitor

6. **Handle security incident**
   - Disable compromised user in Azure AD
   - Revoke tokens and sessions
   - Rotate any exposed secrets
   - Review access logs for lateral movement
   - Notify affected users

---

## Cost Optimization Tips

1. **Use System-Managed Identity** (free) instead of service principals
2. **Consolidate Key Vault** (fewer vaults = lower cost)
3. **Monitor Azure AD license usage** (pay for actual users)
4. **Implement self-service password reset** (reduce support costs)
5. **Use built-in RBAC roles** (cheaper than custom)

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: Authentication, authorization, secrets, compliance
