# Azure Compute Services

Azure Compute provides a range of options for hosting and running applications, from virtual machines to serverless functions. Choose based on your control requirements, scaling needs, and operational preferences.

## Overview

Azure offers multiple compute options, each suited for different scenarios:

### 1. **Virtual Machines (IaaS)**
Full control over OS and runtime. Lift-and-shift legacy apps or custom configurations.
- **Use for**: Legacy apps, custom OS requirements, maximum control
- **Scaling**: Manual or VMSS (auto-scale across instances)
- **Cost Model**: Per-minute, continuous charges
- **Responsibility**: Everything (OS patches, security, updates)

### 2. **App Service (PaaS)**
Managed platform for web apps, APIs, and mobile backends. Built-in services like CI/CD.
- **Use for**: Web apps, REST APIs, microservices (containerized)
- **Scaling**: Auto-scale within tier, horizontal scaling
- **Cost Model**: Per-tier (can be more predictable than VMs)
- **Responsibility**: Application code only, platform managed

### 3. **Azure Functions (Serverless)**
Event-driven code execution. Pay only for compute time used.
- **Use for**: Event-driven workloads, microservices, periodic jobs
- **Scaling**: Automatic, instant scaling
- **Cost Model**: Pay-per-execution (milliseconds)
- **Responsibility**: Function code only
- **Limitation**: 10-60 minute execution timeout

### 4. **Container Instances (ACI)**
Fastest, simplest way to run containers in Azure without Kubernetes.
- **Use for**: Development, testing, simple microservices
- **Scaling**: Manual, no orchestration
- **Cost Model**: Per-second container runtime
- **Simplicity**: No cluster management

### 5. **Azure Kubernetes Service (AKS)**
Production-grade Kubernetes with Azure integration.
- **Use for**: Complex microservices, enterprise workloads, Kubernetes expertise
- **Scaling**: Pod and node auto-scaling
- **Cost Model**: Control plane free, pay for node VMs
- **Responsibility**: Kubernetes management (many options automated)

### 6. **Batch**
Large-scale parallel computing job processing.
- **Use for**: Compute-intensive batch jobs, HPC simulations
- **Scaling**: Massive parallel job execution
- **Cost Model**: Per-core-hour of compute
- **Optimization**: Often using spot instances for cost savings

## Core Concepts

### Scaling Models
- **Vertical**: Bigger machine (limited by max VM size)
- **Horizontal**: More instances (better for most workloads)
- **Auto-scaling**: Automatic horizontal scaling based on metrics

### Availability
- **Single instance**: No SLA
- **Availability Set**: 99.95% SLA within region
- **Availability Zones**: 99.99% SLA across zones
- **Regions**: Geographic redundancy (manual failover)

### Networking
- **Virtual Networks**: Isolation and connectivity
- **Load Balancer**: Layer 4 (TCP/UDP) distribution
- **Application Gateway**: Layer 7 (HTTP/HTTPS) + WAF
- **Private Endpoints**: Eliminate internet exposure

## Pros and Cons

### Virtual Machines
**Pros**: ✅
- Maximum control and flexibility
- Any OS or custom environment
- Good for legacy app migration
- Rich ecosystem of tools

**Cons**: ❌
- Highest operational overhead
- Must patch OS and security
- Highest cost model
- Slower scaling

### App Service
**Pros**: ✅
- Reduced operational overhead
- Built-in CI/CD and scaling
- Good developer experience
- Integrated with Azure services

**Cons**: ❌
- Some runtime limitations
- Less control than VMs
- Can be expensive if not scaled properly
- Memory limits per instance

### Azure Functions
**Pros**: ✅
- Minimal operational overhead
- Automatic, instant scaling
- Pay only for usage
- Easy integration with other services

**Cons**: ❌
- Cold start latency (seconds)
- Duration limits (10-60 min)
- Distributed debugging harder
- Cost can spike with high volume

### Containers (ACI)
**Pros**: ✅
- Simple container deployment
- Seconds to start containers
- No cluster management
- Per-second billing

**Cons**: ❌
- No built-in orchestration
- Manual scaling
- Limited advanced features
- Best for simple workloads only

### Kubernetes (AKS)
**Pros**: ✅
- Industry standard (Kubernetes)
- Sophisticated orchestration
- Great for complex microservices
- Ecosystem of tools and services

**Cons**: ❌
- Steeper learning curve
- More infrastructure complexity
- Higher baseline costs
- Operational complexity

## Real-World Patterns

### 1. **Lift and Shift Migration**
Use VMs to migrate legacy apps as-is, then modernize gradually.
```
Legacy App → Migrate to VMs → Refactor → Migrate to App Service/Functions
```

### 2. **Microservices Architecture**
Deploy each microservice as a container in AKS with automatic scaling.
```
API Gateway → Microservices (AKS) → Storage/Databases
```

### 3. **Event-Driven Pipeline**
Functions triggered by events, minimal infrastructure.
```
Blob Upload → Function (process image) → Storage/Database
```

### 4. **Web App + API Backend**
App Service for public web app, Functions for backend jobs.
```
Frontend (App Service) → Backend APIs (App Service) → Jobs (Functions)
```

### 5. **Batch Processing**
Batch service for large-scale parallel processing.
```
Submit Jobs → Batch processes in parallel → Store results
```

## Performance Tips

### Virtual Machines
- **VM Size**: Choose appropriate size, avoid bottlenecks
- **Caching**: Use Premium SSD for better I/O
- **Proximity**: Use proximity groups to reduce latency
- **Scaling Set**: Use VMSS for auto-scaling multiple VMs

### App Service
- **Tier**: Standard or above for production
- **Auto-scale**: Configure scale rules, test them
- **Warmup**: Long initialization? Use Application Settings for config
- **Local Cache**: Enable for better performance
- **Slots**: Use deployment slots for zero-downtime deployments

### Functions
- **Execution Duration**: Minimize (avoid timeout)
- **Payload Size**: Minimize input/output to reduce serialization
- **Concurrency**: Configure function timeout and max concurrent executions
- **Bindings**: Use bindings (faster) rather than SDK directly
- **Premium Plan**: For better performance and scale

### Containers
- **Image Size**: Smaller images = faster startup
- **Registry**: Use private registry for faster pulls
- **Base Image**: Alpine/lightweight bases preferred
- **Multi-stage**: Multi-stage builds for smaller images

## Common Interview Questions

### 1. **VM vs App Service vs Functions - When to use each?**
**Answer**: VMs for maximum control and legacy apps. App Service for web apps/APIs with built-in scaling. Functions for event-driven, stateless code with usage-based billing.

### 2. **How do you ensure high availability?**
**Answer**: Use Availability Zones for 99.99% SLA, load balancer for distribution, auto-scaling for capacity, and redundancy across regions for disaster recovery.

### 3. **Explain auto-scaling strategy**
**Answer**: Scale out quickly (aggressive rules) to handle load, scale in slowly to avoid thrashing. Monitor actual metrics, test under load. Always set min/max bounds to control costs.

### 4. **What are availability sets vs availability zones?**
**Answer**: Availability Sets distribute VMs across fault domains within region (99.95% SLA). Availability Zones use physically separate datacenters (99.99% SLA). AZs are preferred but may have different pricing.

### 5. **How do containers differ from VMs?**
**Answer**: Containers share host OS kernel (lightweight, seconds to start), VMs have guest OS (isolated but heavier). Containers for microservices, VMs for legacy or full OS isolation.

## Code Examples

See `src/Azure101.Compute/Examples/` for complete code examples (coming soon):
- Virtual Machine management APIs
- App Service deployment and configuration
- Azure Functions examples with multiple triggers
- Container operations
- Scaling and monitoring patterns

## Architecture Decision Guide

```
Need complete control over OS?
├─ Yes → Virtual Machines
└─ No → Need to run code as a service?
    ├─ Yes → Event-driven and stateless?
    │   ├─ Yes → Azure Functions
    │   └─ No → Web app/API?
    │       └─ App Service
    └─ No → Need containers?
        ├─ Simple workload? → Container Instances
        └─ Complex orchestration? → Kubernetes (AKS)
```

## Next Steps

1. **Get Hands-On**: Review code examples for each service
2. **Study Interview Q&A**: Understand differences and use cases
3. **Practice**: Build small projects with different compute options
4. **Scale**: Test auto-scaling and performance monitoring
5. **Cost**: Analyze and optimize compute spending

## Additional Resources

- [Azure Compute Documentation](https://docs.microsoft.com/azure/compute/)
- [Virtual Machines](https://docs.microsoft.com/azure/virtual-machines/)
- [App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Functions](https://docs.microsoft.com/azure/azure-functions/)
- [Container Instances](https://docs.microsoft.com/azure/container-instances/)
- [Azure Kubernetes Service](https://docs.microsoft.com/azure/aks/)
- [Compute Pricing](https://azure.microsoft.com/pricing/details/app-service/)
