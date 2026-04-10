# Azure Compute Services - Complete Wiki

## Overview

Azure Compute provides multiple options for hosting and running applications, from full OS control to fully managed serverless. Choose based on your control requirements, operational overhead tolerance, and performance needs.

---

## Quick Comparison Table

| Service | Type | Use Case | Scaling | Cost | Effort |
|---------|------|----------|---------|------|--------|
| **VM** | IaaS | Full control, legacy apps | Manual/VMSS | Highest | Highest |
| **App Service** | PaaS | Web apps, REST APIs | Auto-scale | Medium | Medium |
| **Functions** | Serverless | Event-driven, short tasks | Auto, instant | Usage-based | Low |
| **ACI** | Containers | Simple containers | Manual | Low | Low |
| **AKS** | Kubernetes | Microservices, complex apps | Auto | Medium-High | High |
| **Batch** | HPC | Parallel computing | Auto | Compute-hours | Medium |

---

## 1. Virtual Machines (VMs)

### What is it?
Infrastructure as a Service (IaaS) - complete control over OS and runtime. You provision, configure, and manage the virtual machine.

### When to Use
- Legacy application migration (lift-and-shift)
- Need custom OS or runtime
- High-performance requirements
- Full network/storage control
- Running multiple workloads on one machine

### Key Features
- **Complete OS Control**: Windows or Linux, any version
- **Sizing Flexibility**: CPU, RAM, storage choices
- **Networking**: Full network configuration control
- **Scalability**: VM Scale Sets for automatic scaling
- **Images**: Preconfigured images or custom images

### Architecture & Core Concepts

#### VM Sizes & Families
- **General Purpose**: B, D series - balanced compute/memory
- **Compute Optimized**: F, H series - high CPU ratio
- **Memory Optimized**: E, M series - high memory ratio
- **Storage Optimized**: L, I series - high disk throughput
- **GPU**: N series - AI/ML workloads

#### Scaling Options
```
Single VM (no SLA)
    ↓
Availability Set (99.95% SLA in single region)
    ↓
Availability Zones (99.99% SLA across zones)
    ↓
VM Scale Set (auto-scaling across instances)
    ↓
Multiple regions (99.99% SLA + disaster recovery)
```

#### Storage Options
- **OS Disk**: System drive (automatically provisioned)
- **Data Disks**: Additional storage, can be shared
- **Temporary Disk**: Not persisted, ephemeral
- **Managed Disks**: Recommended, simplified management
- **Ultra Disk**: High-performance, lowest latency

### Pros & Cons

**Pros** ✅
- Maximum control and flexibility
- Can run any software/OS
- Excellent for legacy applications
- Mature, well-understood technology
- Rich ecosystem and tooling

**Cons** ❌
- Highest operational overhead (patching, updates)
- Manual scaling
- Highest cost per unit
- Security responsibility on you
- Slower startup/shutdown

### Real-World Patterns

#### Pattern 1: Multi-Tier Web Application
```
Load Balancer
    ├─ Web VM 1 (IIS, .NET)
    ├─ Web VM 2 (IIS, .NET)
    └─ Web VM 3 (IIS, .NET)
        ↓
    App VMs (business logic)
        ↓
    Database VM (SQL Server)
```

#### Pattern 2: Dev/Test Environments
Use auto-shutdown to minimize costs. Provision on-demand for testing.

#### Pattern 3: High-Performance Computing
Large VMs with GPU support, scale with VMSS for parallel workloads.

### Performance Tips
- Use managed disks (better performance)
- Choose appropriate VM size (avoid CPU bottlenecks)
- Use accelerated networking for high throughput
- Enable disk caching for read-heavy workloads
- Monitor metrics and right-size if over/under-provisioned

### Cost Optimization
- **Reserved Instances**: 1-3 year commitment saves 30-60%
- **Spot VMs**: Use spare capacity, save up to 90% (non-critical workloads)
- **Auto-shutdown**: Stop VMs outside business hours
- **Monitoring**: Right-size based on actual usage
- **Hybrid Benefit**: Use existing Windows/SQL licenses

### Security Best Practices
- Keep OS and software patched
- Use Managed Identity (no keys/passwords)
- Network Security Groups for access control
- Azure Security Center for recommendations
- Disk encryption for sensitive data
- Use Azure Backup for disaster recovery

---

## 2. App Service

### What is it?
Platform as a Service (PaaS) for hosting web applications and REST APIs. Focus on code, Azure handles infrastructure, OS, runtime patches.

### When to Use
- Web applications and REST APIs
- Mobile backends
- Microservices architecture
- When you want automatic scaling
- When you want reduced operational overhead

### Key Features
- **Built-in Scaling**: Auto-scale based on metrics
- **Multi-Language**: .NET, Java, Node.js, Python, PHP, Ruby
- **CI/CD Integration**: GitHub, Azure DevOps, Bitbucket
- **Networking**: Virtual Network integration, App Gateway
- **Monitoring**: Application Insights integration
- **Staging Slots**: Zero-downtime deployments

### Pricing Tiers

| Tier | Purpose | Auto-Scale | SLA | Cost |
|------|---------|------------|-----|------|
| **Free** | Dev/Test | No | None | Free (shared) |
| **Shared** | Dev/Test | No | None | Cheap (shared) |
| **Basic** | Dev/Test | No | None | $ |
| **Standard** | Production | Yes | 99.95% | $$ |
| **Premium** | Enterprise | Yes | 99.95% | $$$ |
| **Isolated** | Enterprise | Yes | 99.99% | $$$$ |

### Architecture & Core Concepts

#### Deployment Models
- **Code Deployment**: Git, GitHub, Azure DevOps, local Git
- **Containerized**: Docker containers from registry
- **ZIP Deploy**: Upload ZIP file directly
- **Continuous Deployment**: Auto-deploy on source code changes

#### Scaling
```
Scaling Up (Vertical): Increase tier (more powerful machines)
    vs
Scaling Out (Horizontal): Increase instance count (more machines)
```

#### Deployment Slots
```
Production Slot (live, users access)
    ↕ Swap (instant, zero downtime)
Staging Slot (test new version)
```

### Real-World Patterns

#### Pattern 1: Web App + API Backend
```
Azure Front Door (CDN, global routing)
    ↓
App Service (Web app, multiple instances)
    ├─ Deployment slot for staging
    └─ Integrated with Key Vault, Database
```

#### Pattern 2: Microservices
```
API Management (gateway)
    ├─ User Service (App Service)
    ├─ Product Service (App Service)
    ├─ Order Service (App Service)
    └─ Payment Service (App Service)
```

### Performance Tips
- Use Standard tier or above for production
- Configure auto-scale rules appropriately
- Enable Application Insights for monitoring
- Use deployment slots for zero-downtime updates
- Configure connection pooling in application
- Enable local cache for better performance
- Use CDN for static assets

### Cost Optimization
- Right-size tier based on needs
- Use auto-scale to match demand
- Reserved capacity (cheaper than pay-as-you-go)
- Turn off staging slots when not needed
- Monitor actual resource usage
- Use Free or Shared for dev/test

### Security Best Practices
- Use managed identity (no connection strings in code)
- Enable HTTPS only
- Use IP restrictions or Virtual Network
- Enable authentication (App Service built-in or custom)
- Store secrets in Key Vault
- Keep application dependencies updated
- Monitor authentication failures

---

## 3. Azure Functions

### What is it?
Serverless computing - write code that runs in response to events without managing infrastructure. Pay only for execution time.

### When to Use
- Event-driven workloads
- Scheduled tasks (timers)
- Data processing/transformation
- Real-time message processing
- Lightweight microservices
- Glue code between services

### Key Features
- **No Infrastructure**: Automatic scaling, no servers to manage
- **Event-Driven**: HTTP, Timer, Queue, Blob, Cosmos DB, Service Bus, Event Hub
- **Multiple Languages**: C#, Python, JavaScript, Java, PowerShell
- **Bindings**: Simplified integration with Azure services
- **Stateless**: Best for short-lived operations
- **Cost**: Pay per execution millisecond

### Hosting Plans

| Plan | Auto-Scale | Duration | Concurrency | Cost |
|------|-----------|----------|------------|------|
| **Consumption** | Instant | 10 min | Unlimited | Per execution |
| **Premium** | Seconds | 60 min | Reserved | Per vCore-hour |
| **Dedicated** | Manual | 60 min | Limited | Per App Service tier |

### Trigger Types

```
HTTP Triggers: RESTful endpoints
Queue Triggers: Azure Queue Storage messages
Blob Triggers: Object storage changes
Timer Triggers: Scheduled execution
Service Bus: Queue/Topic messages
Event Hub: Streaming data
Event Grid: Resource events
Cosmos DB: Database changes
```

### Architecture & Core Concepts

#### Cold Starts
- First invocation after idle: 0.5-3+ seconds
- Premium plan: faster, consistent
- Consumption plan: coldest, cheapest
- Keep functions simple to reduce startup time

#### Execution Context
```
Function triggered
    ↓
Function runs (start to finish)
    ↓
Result returned/processed
    ↓
Function idle (no cost)
```

### Real-World Patterns

#### Pattern 1: Event Processing Pipeline
```
Blob Upload Event Grid
    ↓
Function (process image)
    ↓
Outputs: Cosmos DB + Storage
```

#### Pattern 2: Scheduled Tasks
```
Timer Trigger (daily at 3am)
    ↓
Function (cleanup old data)
    ↓
Logs to Application Insights
```

#### Pattern 3: Microservice Backend
```
HTTP Trigger Function (API endpoint)
    ↓
Call other services via bindings
    ↓
Return response
```

### Performance Tips
- Keep functions simple and focused
- Minimize dependencies
- Use bindings (faster than SDK calls)
- Avoid cold starts with Premium plan
- Monitor execution times with Application Insights
- Use async/await throughout
- Batch operations when possible

### Cost Optimization
- Consolidate functions to reduce cold starts
- Use bindings instead of SDK (less overhead)
- Monitor execution time and optimize
- Use consumption plan (cheapest, auto-scales)
- Premium plan if cold starts are critical
- Reserved capacity for predictable load

### Security Best Practices
- Use managed identity for authentication
- Get secrets from Key Vault, not environment
- Validate all inputs
- Use HTTPS only for HTTP triggers
- Enable Azure Security Center
- Monitor function execution logs
- Implement retry logic with backoff

---

## 4. Container Instances (ACI)

### What is it?
Simplest way to run containers in Azure without managing cluster infrastructure. Fastest container deployment.

### When to Use
- Running individual containers
- Development and testing
- Batch processing/scheduled tasks
- Simple microservices
- When you don't need Kubernetes

### Key Advantages
- Seconds to start (not minutes)
- No cluster management
- Per-second billing
- Single command deployment
- Simple networking

### Performance Tips
- Choose appropriate CPU/memory
- Use multi-container groups for related services
- Persistent storage via mounted volumes
- Monitor with Log Analytics

---

## 5. Azure Kubernetes Service (AKS)

### What is it?
Managed Kubernetes service for orchestrating containers. Production-grade platform for complex microservices.

### When to Use
- Complex microservices architecture
- Need sophisticated orchestration
- Your team knows Kubernetes
- Need service mesh (Istio, Linkerd)
- Enterprise workloads requiring HA

### Key Features
- Managed control plane (Azure handles updates)
- Auto-scaling (pod and node level)
- Service mesh integration
- Network policies
- Storage integration
- Monitoring with Container Insights

### Architecture Considerations
- Control plane: Managed by Azure (free)
- Worker nodes: You pay for VMs
- Minimum cost: 1 control plane + 3 worker nodes
- Higher operational complexity than App Service
- Kubernetes expertise required

### When NOT to Use AKS
- Single simple application → Use App Service
- Event-driven workload → Use Functions
- Limited Kubernetes expertise → Use App Service
- Cost-sensitive → Use App Service or Functions
- Simple containerized app → Use ACI

---

## 6. Batch

### What is it?
Large-scale parallel computing service for batch job processing. Automatically scales compute resources.

### When to Use
- Parallel computing jobs (HPC)
- Large-scale data processing
- Scientific simulations
- Rendering/encoding jobs
- When you need thousands of cores

### Key Advantages
- Scales to thousands of cores
- Automatic resource management
- Spot instances for cost savings (up to 90%)
- Task scheduling and dependencies
- Monitoring and logging

---

## Compute Decision Tree

```
Need full OS control?
├─ Yes → Virtual Machines
│   ├─ Many small apps? → VM Scale Set
│   ├─ Need HA? → Availability Zones
│   └─ Cost-conscious? → Spot VMs, Reserved
└─ No → Need to run code without thinking about infrastructure?
    ├─ Yes → Serverless
    │   ├─ Event-driven? → Azure Functions
    │   ├─ HTTP endpoint? → Function with HTTP trigger
    │   └─ Scheduled task? → Function with Timer trigger
    └─ No → Web app or microservices?
        ├─ Single app or simple microservices? → App Service
        │   ├─ Need auto-scale? → Standard+ tier
        │   ├─ Multiple environments? → Deployment Slots
        │   └─ Cost-conscious dev/test? → Free/Shared tier
        └─ Complex microservices?
            ├─ Team knows Kubernetes? → AKS
            ├─ Simple containers? → Container Instances
            └─ No container experience? → App Service + containers
```

---

## Common Scenarios

### Scenario 1: Migrating Legacy Windows App
**Solution**: Virtual Machines
- Use Windows VM with SQL Server
- Keep existing code unchanged
- Gradually modernize

### Scenario 2: Building Modern Web App
**Solution**: App Service + Functions + Storage
- App Service for frontend/API
- Functions for background processing
- Blob Storage for files

### Scenario 3: Real-Time Data Processing
**Solution**: Functions + Event Hubs
- Event Hubs ingests streaming data
- Functions process in real-time
- Output to database/storage

### Scenario 4: Microservices at Scale
**Solution**: AKS
- Kubernetes for orchestration
- Service mesh for communication
- Auto-scaling for demand

---

## Comparison: When to Upgrade

```
App Service Limitations?
├─ Need more than 8 cores? → Premium Isolated
├─ Need lower latency? → Premium (reserved instances)
├─ Outgrowing single app? → AKS (microservices)
├─ Unpredictable load? → Add Functions for async
└─ All else failing? → Virtual Machines (full control)

Functions Limitations?
├─ Timeout issues (>10min)? → Premium plan
├─ High latency needed? → App Service
├─ Need state? → App Service + storage
├─ Complex orchestration? → AKS or Logic Apps
└─ Lots of dependencies? → App Service or ACI
```

---

## Interview Questions

### Q1: When would you choose App Service over Functions?
**A**: App Service for long-running apps, stateful applications, or when you need more control. Functions for event-driven, short-lived operations. If uncertain, App Service is safer.

### Q2: Explain VM vs. Container vs. Serverless trade-offs
**A**: 
- VMs: Maximum control, highest overhead, highest cost
- Containers: Good balance with orchestration (AKS)
- Serverless: Minimal overhead, auto-scaling, usage-based pricing

### Q3: How do you handle Functions that need to run longer than 10 minutes?
**A**: Upgrade to Premium plan (60 min max) or break into smaller functions. For longer tasks, use App Service or background jobs in App Service.

### Q4: What's the cost difference between App Service tiers?
**A**: Free = shared resources, no SLA. Shared = still shared. Basic and above = dedicated instances. Standard = auto-scale. Premium = better performance. Isolated = highest security/performance.

### Q5: How do you scale a monolithic application on Azure?
**A**: 
1. First: App Service Standard+ with auto-scale rules
2. If still insufficient: Premium Isolated
3. If architectural limit: Refactor to microservices on AKS

---

## Additional Resources

- [Official Compute Documentation](https://docs.microsoft.com/azure/compute/)
- [VM Best Practices](https://docs.microsoft.com/azure/virtual-machines/windows/how-to-enable-write-accelerator)
- [App Service Scaling](https://docs.microsoft.com/azure/app-service/manage-scale-up)
- [Functions Best Practices](https://docs.microsoft.com/azure/azure-functions/functions-best-practices)
- [AKS Best Practices](https://docs.microsoft.com/azure/aks/best-practices)

---

**Last Updated**: April 2026  
**Status**: Complete  
**Difficulty**: Beginner to Advanced
