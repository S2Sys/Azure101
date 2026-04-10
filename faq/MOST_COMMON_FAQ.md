# Most Common Azure FAQ - Top 50 Questions

Quick answers to the most frequently asked Azure questions from developers, architects, and DevOps engineers.

---

## Service Selection (10 Questions)

### Q1: I'm building a web application. What should I use?
**A**: **App Service (recommended)** - Good balance of ease-of-use and features. Use Functions for background jobs, Storage for files.

**Alternatives**:
- VM: If you need complete control or legacy app
- AKS: If you have microservices architecture
- Static Web Apps: If frontend-only with serverless backend

---

### Q2: How do I choose between Blob Storage and File Shares?
**A**: 
- **Blob Storage**: Unstructured data (images, videos, logs, backups), direct HTTP/REST access, higher throughput
- **File Shares**: File system semantics, SMB/NFS protocol, shared access from multiple VMs, Windows File Explorer compatible

**Simple rule**: Use Blob for APIs/archives, Files for shared network drives.

---

### Q3: Should I use SQL Database or Cosmos DB?
**A**:
- **SQL Database**: Relational data, ACID transactions, complex queries, cost-effective. **Default choice**
- **Cosmos DB**: Global distribution needed, massive scale, flexible schema, lowest latency anywhere, higher cost

**Decision flow**:
- Relational data + single region? → **SQL Database**
- Relational data + global? → **SQL Database with geo-replication**
- NoSQL needed + global? → **Cosmos DB**
- Unsure? → **Start with SQL Database**

---

### Q4: What's the difference between Azure Functions and Logic Apps?
**A**:
- **Functions**: Code-first, C#/Python/Node.js, fast execution, good for performance-critical code
- **Logic Apps**: Visual workflow, low-code/no-code, good for business processes and integrations

**Choose Functions if**: You're writing code
**Choose Logic Apps if**: Non-technical person designing workflow, or B2B integration

---

### Q5: Should I use Virtual Machines or App Service?
**A**:
| Need | VM | App Service |
|------|----|----|
| Full OS control | ✅ | ❌ |
| Custom runtime | ✅ | ❌ |
| Auto-scaling | ⚠️ (VMSS) | ✅ |
| Simple web app | ❌ | ✅ |
| Legacy migration | ✅ | ❌ |
| Cost | Higher | Lower |
| Operational overhead | High | Low |

**Rule of thumb**: Use App Service first, VM only if App Service won't work.

---

### Q6: What compute option should I use for small workload?
**A**: In order of preference:
1. **Functions** (cheapest if usage is <10s/month)
2. **App Service Shared/Basic** ($~10/month)
3. **Container Instances** (per-second billing)
4. **Single VM** (minimum ~$15-30/month)

---

### Q7: How do I choose between Service Bus and Event Hubs?
**A**:
- **Service Bus**: Reliable messaging, queues + topics, guaranteed delivery, good for request-response
- **Event Hubs**: Streaming events, high throughput, good for telemetry and event sources

**Service Bus if**: You need reliable messaging and subscriptions
**Event Hubs if**: You have high-volume event stream

---

### Q8: Should I use containers?
**A**: Only if:
- ✅ You have Docker expertise
- ✅ You need to bundle complex dependencies
- ✅ You want consistent dev-to-prod environment
- ✅ You're building microservices

**Don't use containers if**:
- ❌ Simple web app → Use App Service
- ❌ Single small app → Use App Service
- ❌ Team doesn't know Docker → Use App Service

---

### Q9: How do I reduce Azure costs?
**A**: In order of impact:
1. **Right-size resources** (biggest impact) - Use actual usage data
2. **Use Reserved Instances** for predictable workloads (30-60% savings)
3. **Turn off unused resources** - VMs, databases not in use
4. **Use Spot VMs** for non-critical workloads (up to 90% savings)
5. **Choose cheaper regions** - East US is cheaper than most others
6. **Use free/shared tiers** for dev/test
7. **Enable auto-shutdown** for dev resources
8. **Monitor with Cost Management** and set budgets

---

### Q10: What are the Azure regions and how do I choose?
**A**: **Primary factors**:
1. **Latency**: Choose closest to users
2. **Compliance**: Data residency requirements
3. **Cost**: Some regions are cheaper (East US < West Europe)
4. **Service availability**: Not all services in all regions

**Recommended**: East US (cheapest), then choose based on location.
**For HA**: Use 2+ regions minimum, preferably paired regions (East US + West US 2).

---

## Authentication & Security (8 Questions)

### Q11: How do I authenticate to Azure?
**A**: Multiple ways in priority order:
1. **Managed Identity** (preferred, no keys needed) - Only in Azure
2. **DefaultAzureCredential** (tries multiple methods) - Dev/test
3. **Service Principal** (ClientSecretCredential) - Apps, automation
4. **Connection Strings** (⚠️ less secure) - Fallback only
5. **Storage Keys** (❌ avoid) - Never put in code

**Best practice**: Use managed identity in production, DefaultAzureCredential locally.

---

### Q12: Where should I store passwords and connection strings?
**A**: 
- **Production**: Azure Key Vault (encrypted, audited access)
- **Local dev**: User secrets (`dotnet user-secrets`)
- **CI/CD**: Azure Key Vault or GitHub Secrets
- **Never**: Hardcoded in source, environment variables in code

---

### Q13: What's the difference between Storage Keys, Shared Access Signatures (SAS), and Managed Identity?
**A**:
- **Keys**: Full access, permanent, should be in Key Vault
- **SAS**: Limited access, time-limited, specific permissions, good for temporary access
- **Managed Identity**: Preferred, only in Azure, automatic credential management

**When to use each**:
- External users/APIs? → SAS
- App running in Azure? → Managed Identity
- Legacy code? → Keys in Key Vault
- Development? → DefaultAzureCredential

---

### Q14: How do I secure storage accounts?
**A**: Security layers:
1. Use **Managed Identity** instead of keys
2. Enable **firewall** (restrict to specific networks)
3. Use **private endpoints** to avoid internet exposure
4. Enable **encryption at rest** (default)
5. Use **HTTPS only** (default)
6. Implement **RBAC** (role-based access)
7. Use **SAS** tokens for temporary access
8. Enable **audit logging** (optional, extra cost)

---

### Q15: What's the difference between RBAC and ABAC?
**A**:
- **RBAC (Role-Based)**: User has role (Reader, Contributor, Owner)
- **ABAC (Attribute-Based)**: Fine-grained conditions (e.g., "access only during business hours")

**Most organizations use**: RBAC for simplicity, some roles with ABAC conditions for complex scenarios.

---

### Q16: How do I enable multi-factor authentication (MFA)?
**A**: 
- **Azure Portal**: Azure Security → MFA settings
- **Microsoft Entra ID (AAD)**: Conditional access policies
- **Within applications**: Implement custom MFA logic

**Recommendation**: Enable for all administrative accounts, conditional access for users.

---

### Q17: How do I audit who accessed my data?
**A**: 
- **Storage accounts**: Enable "Storage Analytics" logging (per-operation logs)
- **Databases**: Enable audit logging
- **Azure Monitor**: View activities in Activity Log
- **Log Analytics**: Aggregate logs across resources

**For compliance**: Use Azure Synapse for audit analysis.

---

### Q18: How do I encrypt data at rest and in transit?
**A**:
- **At Rest**: Enable encryption (mostly default)
  - Storage accounts: Encryption enabled by default
  - Databases: Enable transparent data encryption (TDE)
  - Disks: Enable encryption
- **In Transit**: Use HTTPS/TLS (enforced by Azure services)
- **End-to-End**: Customer-managed keys in Key Vault

---

## Cost Management (8 Questions)

### Q19: How are Azure resources billed?
**A**: Different models per service:
- **Compute**: Per minute (VMs), per tier (App Service), per execution (Functions)
- **Storage**: Per GB stored + per GB transferred out
- **Databases**: Per DTU/vCore + storage
- **Networking**: Per GB data transfer out (ingress free)

**Key point**: Egress (data out) is expensive, ingress (data in) is free.

---

### Q20: What's the cheapest way to store backups?
**A**: Cost comparison (per TB/month):
1. **Archive tier**: ~$1 (but retrieval is slow/expensive)
2. **Cool tier**: ~$10
3. **Hot tier**: ~$23

**Strategy**: 
- Recent backups → Cool tier
- Old backups → Archive tier (moving costs are low)
- Use lifecycle policies to automate

---

### Q21: How much does data transfer cost?
**A**: Pricing (per GB):
- **Ingress**: Free
- **Egress within region**: Usually free
- **Egress between regions**: ~$0.05/GB
- **Egress to internet**: ~$0.20/GB

**Cost optimization**: 
- Use CDN for public content (cheaper egress)
- Keep related services in same region

---

### Q22: What are Reserved Instances and should I use them?
**A**: 
**Reserved Instance**: Pre-pay for 1-3 years, get 30-60% discount.

**Use if**:
- ✅ Baseline workload that won't decrease
- ✅ Committed to service for 1+ years
- ✅ Predictable load

**Don't use if**:
- ❌ Highly variable workload
- ❌ May shut down service
- ❌ Evaluating (wait until certain)

**Recommendation**: Buy reserved for 70-80% of baseline, use pay-as-you-go for variable.

---

### Q23: Are there free resources in Azure?
**A**: Yes! Azure Free Tier includes:
- 12 months free: App Service Basic, SQL Database (11GB)
- Always free: Functions (1M executions), Storage (5GB), Cosmos DB, Log Analytics

**Total potential savings**: ~$200-300/month with free tier.

---

### Q24: How do I monitor my Azure costs?
**A**: 
- **Azure Portal**: Cost Management + Billing blade
- **Budgets**: Set alerts when spending reaches threshold
- **Advisor**: Recommendations for cost optimization
- **Reserved Instance commitment**: Plan purchases

**Best practice**: Set $0 budget for dev/test, review costs weekly.

---

### Q25: What's the difference between billing by consumption vs. commitment?
**A**:
- **Consumption**: Pay per unit used (cheapest initially, can spike)
- **Reserved**: Pre-pay, deep discount (best long-term)
- **Spot**: Bid for spare capacity, up to 90% off

**Strategy**: Baseline with Reserved, growth with consumption.

---

### Q26: How do I compare pricing between cloud providers?
**A**: Use **Azure Pricing Calculator** for detailed estimates. Generally:
- **Azure**: Cheapest for Microsoft stack (.NET, SQL Server, Office 365)
- **AWS**: Cheapest for Unix/Linux-based workloads
- **GCP**: Good for data analytics and machine learning

**Best approach**: Estimate your actual workload in each, compare.

---

## Performance & Optimization (8 Questions)

### Q27: Why is my application slow?
**A**: Diagnosis steps:
1. **Check metrics**: CPU, memory, network utilization
2. **Check network latency**: Might be regional distance
3. **Check database**: Slow queries are common culprit
4. **Check logging**: Application Insights for exceptions/slowness
5. **Check dependencies**: Slow external APIs

**Most common**: Database queries (add caching/indexing) or undersized compute.

---

### Q28: How do I cache data in Azure?
**A**: Options by use case:
- **Redis Cache**: In-memory key-value (session data, frequently accessed)
- **CDN**: Static content caching (images, JS, CSS)
- **Application-level**: Cache within app (in-memory dictionary)
- **Database caching**: Query results
- **Service bus**: Message batching

**Recommendation**: Redis for shared cache, in-memory for local cache.

---

### Q29: How do I monitor application performance?
**A**: Use **Application Insights**:
- Automatic metrics (response time, failure rate)
- Custom metrics (business events)
- Distributed tracing (follow requests across services)
- Alerting (notify on degradation)

**Basic setup**: 
```csharp
// Add to startup
services.AddApplicationInsightsTelemetry();
```

---

### Q30: What's a good response time for a web application?
**A**:
- **Excellent**: < 200ms
- **Good**: 200-500ms
- **Acceptable**: 500-1000ms
- **Poor**: > 1000ms

**Factors**: Network latency, database queries, external APIs, geographic distance.

---

### Q31: How do I scale my application?
**A**: Three types:
1. **Vertical scaling** (bigger machine): Easier but limited
2. **Horizontal scaling** (more machines): Better, requires load balancing
3. **Auto-scaling** (automatic): Best for variable load

**Best practice**: Auto-scale based on CPU/memory metrics.

---

### Q32: How do I handle database scaling?
**A**: 
- **Read scaling**: Read replicas, caching
- **Write scaling**: Sharding (split data), or upgrade DTU/vCore
- **Backup**: Replication in different region

**SQL Database**: Scale up (vCore size) → then scale out (read replicas, geo-replication).

---

### Q33: What's a CDN and should I use one?
**A**: **CDN (Content Delivery Network)**: Caches content near users globally, reduces latency and bandwidth costs.

**Use if**:
- ✅ Serving static content (images, JS, CSS)
- ✅ Global audience
- ✅ Content changes infrequently

**Example cost**: CDN save 50-80% on egress for global content.

---

### Q34: How do I optimize database queries?
**A**: 
1. **Add indexes** (biggest impact, 100x+ speedup possible)
2. **Avoid N+1 queries** (fetch related data in one query)
3. **Use query execution plans** (identify slow queries)
4. **Partition data** (query only needed data)
5. **Archive old data** (keep tables small)
6. **Use denormalization** (careful trade-off)

**Most common issue**: Missing indexes on filter columns.

---

## Operational & Deployment (8 Questions)

### Q35: What's Infrastructure as Code and why use it?
**A**: **IaC**: Define infrastructure in code (JSON, YAML, HCL), version control it, deploy automatically.

**Benefits**:
- Reproducible (same environment every time)
- Version controlled (track changes)
- Documented (code is documentation)
- Automated (fast deployment, fewer errors)

**Azure options**: 
- **Bicep** (recommended, simpler than ARM)
- **Terraform** (multi-cloud)
- **ARM Templates** (official Azure)
- **Pulumi** (code-centric)

---

### Q36: How do I set up CI/CD?
**A**: Steps:
1. **Source control**: Git (GitHub, Azure Repos)
2. **CI pipeline**: Build, test on every commit
3. **CD pipeline**: Deploy to staging, then production
4. **Testing**: Unit, integration, smoke tests
5. **Monitoring**: Rollback if health degrades

**Azure services**: Azure Pipelines (recommended) or GitHub Actions.

---

### Q37: How do I deploy an application to Azure?
**A**: Multiple options (by frequency):
1. **Visual Studio** (F5 publish) - One-time/testing
2. **Azure CLI** (`az webapp deployment`) - Manual, rare
3. **Git deployment** (push to trigger deploy) - Frequent
4. **GitHub Actions** (auto-deploy on PR merge) - Recommended
5. **Azure Pipelines** (complex scenarios)

**Recommended**: GitHub Actions or Azure Pipelines for any production deployment.

---

### Q38: What's a staging slot and why use one?
**A**: **Staging slot**: Separate instance of your app for testing before production.

**Benefits**:
- Test in production environment (same config)
- Zero-downtime deployments (swap after verification)
- Instant rollback (swap back if issues)

**Usage**: 
```
New version → Deploy to staging
            → Warm up (run tests)
            → Swap with production
            → Monitor
            → If problems, swap back
```

---

### Q39: How do I roll back a deployment?
**A**: Methods:
1. **Deployment slot swap**: Instant rollback (recommended)
2. **Redeploy previous version**: Simple, takes a few minutes
3. **Blue-green deployment**: Two environments, switch traffic
4. **Database**: Backups should be automatic (PITR)

**Best practice**: Deployment slots for zero-downtime rollback.

---

### Q40: How do I debug an issue in production?
**A**: Tools & approach:
1. **Application Insights**: View exceptions, trace requests
2. **Log Analytics**: Aggregate logs, custom queries
3. **Metrics**: Check CPU, memory, network
4. **Network traces**: Wireshark for network issues
5. **Slow query logs**: For database issues
6. **Remote debugging**: Attach debugger (risky in production, use carefully)

**Prevention**: Comprehensive logging, monitoring, staged deployments.

---

### Q41: How do I handle blue-green deployments?
**A**: Approach:
```
Blue (Current) → Running production
Green (New) → New version, tested, warming up
            → Traffic switched to Green
            → Blue kept as rollback
```

**Implementation**: 
- Use deployment slots
- Or use multiple App Service instances behind load balancer
- Or use Kubernetes with service mesh

---

### Q42: What's the difference between immutable and mutable deployments?
**A**:
- **Immutable**: Deploy new instances, never change existing (recommended)
- **Mutable**: Update existing instances in place (risky, can be inconsistent)

**Why immutable is better**: Predictable, easy rollback, easy scaling.

---

## Disaster Recovery & Availability (6 Questions)

### Q43: How do I protect against data loss?
**A**: Layered approach:
1. **Automatic backups** (all Azure services have these)
2. **Geo-redundant storage** (replication across regions)
3. **Point-in-time recovery** (PITR) - restore to any point in last 35 days
4. **Export/archive** (long-term retention)
5. **Offline backup** (Azure Data Box for massive data)

**Recommendation**: Geo-redundant + PITR minimum for production.

---

### Q44: What's an SLA and what does 99.9% uptime mean?
**A**: 
**SLA (Service Level Agreement)**: Azure's promise of uptime.
- **99.9%**: ~43 minutes downtime/month
- **99.95%**: ~22 minutes downtime/month
- **99.99%**: ~4 minutes downtime/month

**To achieve higher SLA**: Use multiple regions, redundancy, load balancing.

---

### Q45: How do I set up disaster recovery?
**A**: Components:
1. **RTO (Recovery Time Objective)**: How fast to recover (target: <1 hour)
2. **RPO (Recovery Point Objective)**: How much data loss acceptable (target: <5 min)
3. **Secondary region**: Failover location (paired regions recommended)
4. **Replication**: Geo-redundant replication (automatic or manual)
5. **Testing**: Regularly test failover (once/quarter minimum)

**Azure tools**: Azure Site Recovery (for VMs), Geo-replication (for databases).

---

### Q46: What's the difference between active-active and active-passive?
**A**:
- **Active-Active**: Both regions handling traffic, distributed
- **Active-Passive**: Only primary active, secondary on standby

**Active-Active**: Higher cost, zero failover time, more complex.
**Active-Passive**: Lower cost, failover takes minutes, simpler.

**Recommendation**: Active-passive for most apps, active-active for critical systems.

---

### Q47: How do I ensure high availability?
**A**: Checklist:
- ✅ Use Availability Zones (99.99% SLA)
- ✅ Multiple instances behind load balancer
- ✅ Database geo-replication
- ✅ Static content on CDN
- ✅ Retry logic with exponential backoff
- ✅ Circuit breaker pattern for external APIs
- ✅ Health checks and auto-recovery
- ✅ Monitoring and alerting

**Target**: 99.99% uptime (4 minutes downtime/month).

---

### Q48: How do I monitor health of my application?
**A**: 
- **Health checks**: Endpoint that returns status
- **Heartbeat**: Regular ping to verify availability
- **Synthetic monitoring**: Simulate user transactions
- **Alert rules**: Notify on degradation
- **Dashboard**: Real-time view of key metrics

**Implementation**: Application Insights + custom health endpoints.

---

## Advanced/Misc (2 Questions)

### Q49: What's the difference between Azure Stack and Azure?
**A**:
- **Azure**: Cloud service (Microsoft-managed)
- **Azure Stack**: On-premises (you manage), same services as Azure
- **Azure Arc**: Extend Azure management to on-premises

**Use Azure Stack if**: You need on-premises or datacenter deployment.

---

### Q50: How do I integrate Azure with on-premises systems?
**A**: Options:
1. **VPN Gateway**: Encrypted site-to-site connection
2. **ExpressRoute**: Dedicated private connection
3. **Azure Arc**: Extend Azure management on-premises
4. **Service Bus Relay**: Connect through firewall
5. **Azure Data Factory**: Data integration

**Most common**: ExpressRoute for production, VPN for dev/test.

---

## Summary

**Most critical concepts**:
1. Choose appropriate compute (Functions < App Service < VMs)
2. Use managed identities for authentication
3. Store secrets in Key Vault
4. Monitor costs aggressively
5. Use Infrastructure as Code
6. Implement CI/CD for all deployments
7. Plan for disaster recovery
8. Monitor application performance

**Best practices**:
- Start simple, scale as needed
- Use auto-scaling for variable workloads
- Implement comprehensive logging/monitoring
- Test disaster recovery procedures
- Review costs monthly
- Keep Azure SDK dependencies updated

---

**Last Updated**: April 2026  
**Difficulty**: Beginner to Intermediate  
**Purpose**: Quick reference for common questions
