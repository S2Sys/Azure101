# Azure DevOps & Monitoring Services - Complete Wiki

## Overview

Azure DevOps provides CI/CD pipelines, version control, monitoring, and logging. Essential for modern application development and operations.

---

## Quick Comparison Table

| Service | Purpose | Type | Cost | Best For |
|---------|---------|------|------|----------|
| **Azure Pipelines** | CI/CD automation | Build/Release | Free (1800 min/month) | Automated deployments |
| **Repos** | Version control | Git | Free | Source code management |
| **Application Insights** | APM | Monitoring | Per GB ingested | Application performance |
| **Log Analytics** | Log aggregation | Logging | Per GB ingested | Centralized logging |
| **Azure Monitor** | Infrastructure monitoring | Metrics | Per metric/alert | VM/resource metrics |
| **DevTest Labs** | Test environments | Lab management | Per VM/artifact | Dev/test resource management |

---

## 1. Azure Pipelines

### What is it?
Continuous Integration/Continuous Deployment (CI/CD) service that automatically builds, tests, and deploys code on every commit.

### When to Use
- Automate building and testing
- Deploy on every code change
- Run tests before production
- Schedule deployments
- Gate deployments with approvals

### Key Features
- **Triggers**: Build on code commit, schedule, manual
- **Agents**: Self-hosted or Microsoft-hosted
- **Tasks**: 500+ pre-built tasks (build, test, deploy)
- **Stages**: Separate build, test, staging, production
- **Gates**: Approvals before deployment
- **Artifacts**: Store build outputs

### Architecture & Core Concepts

#### Pipeline Flow
```
1. Developer commits code
   └─ GitHub/Azure Repos

2. Trigger builds automatically
   └─ Build stage
   ├── Compile code
   ├── Run unit tests
   └── Create artifact

3. Test stage
   ├── Integration tests
   ├── Performance tests
   └── Security scans

4. Approval gate
   ├── Human approval required
   └── Or automated quality gates

5. Deploy stage
   ├── Deploy to staging
   ├── Deploy to production
   └── Health checks

6. Rollback (if needed)
   └── Automatic or manual rollback
```

#### YAML Pipeline Example
```yaml
trigger:
  - main

stages:
- stage: Build
  jobs:
  - job: BuildJob
    steps:
    - task: DotNetCoreCLI@2
      inputs:
        command: build
    - task: DotNetCoreCLI@2
      inputs:
        command: test

- stage: Deploy
  condition: succeeded()
  jobs:
  - deployment: Deploy
    environment: production
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebApp@1
            inputs:
              appName: 'myapp'
              package: '$(Pipeline.Artifact)'
```

### Pros & Cons

**Pros** ✅
- Fully managed (no infrastructure to maintain)
- 1800 free minutes per month
- Unlimited private repositories
- Easy GitHub integration
- YAML-based (version controlled)
- Comprehensive pre-built tasks

**Cons** ❌
- Learning curve for complex pipelines
- Can be slow for large builds
- Artifacts storage costs
- Limited parallelization in free tier
- Debugging can be difficult

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Free tier minutes/month | 1800 |
| Paid tier minutes/month | 10000+ |
| Concurrent jobs (free) | 1 |
| Concurrent jobs (paid) | 10+ |
| Artifact retention | 30 days (configurable) |
| Pipeline timeout | 360 minutes |

### Real-World Use Cases

#### Use Case 1: Automated .NET Application Deployment
```
Developer pushes code to main branch
    ↓
Trigger: Build + Test pipeline
    ├── Compile C# code
    ├── Run unit tests
    ├── Run integration tests
    ├── SonarQube quality gate
    └── Create zip artifact

    ↓
Deploy to Staging
    ├── Deploy artifact to App Service
    ├── Run smoke tests
    └── Wait for approval

    ↓
Manual Approval (Required)
    └── Product Manager approves

    ↓
Deploy to Production
    ├── Blue-green deployment
    ├── Health checks
    └── Automatic rollback if unhealthy

Result: Zero-downtime deployment
```

#### Use Case 2: Docker Image Pipeline
```
Developer pushes Dockerfile to repo
    ↓
Build stage
    ├── Build Docker image
    ├── Run security scan (Trivy)
    ├── Tag with build number
    └── Push to Container Registry

Deploy stage
    ├── Deploy image to AKS
    ├── Run integration tests
    ├── Monitor for errors
    └── Automatic rollback if critical errors
```

### Performance Tips
- Use self-hosted agents for large builds
- Parallelize tests across jobs
- Cache dependencies (NuGet packages)
- Use artifact caching
- Implement quality gates (don't deploy bad builds)
- Monitor pipeline execution times

### Cost Optimization
- Use free tier (1800 min/month)
- Clean up old artifacts
- Use lightweight agents
- Parallelize to reduce total time
- Schedule non-critical builds during off-hours

---

## 2. Application Insights

### What is it?
Application Performance Monitoring (APM) service that tracks application health, performance, and user behavior.

### When to Use
- Monitor application health
- Track performance metrics
- Debug production issues
- Understand user behavior
- Performance optimization

### Key Features
- **Metrics**: Request rate, response time, failure rate
- **Traces**: Detailed request flow with timing
- **Logs**: Application logs aggregated
- **Alerts**: Notify on anomalies
- **Dashboards**: Visualize metrics
- **Analytics**: Query performance data

### Architecture & Core Concepts

#### What Gets Tracked
```
Requests
├── URL path
├── HTTP status (200, 500, etc)
├── Response time (milliseconds)
└── Server processing time

Dependencies
├── Database queries (time, success/failure)
├── HTTP calls to other services
├── Cache hits/misses

Exceptions
├── Unhandled exceptions
├── Stack traces
├── Frequency

Custom Events
├── "User Registered"
├── "Purchase Completed"
├── "Feature Flag Toggled"
```

### Pros & Cons

**Pros** ✅
- Automatic instrumentation (add 1 line of code)
- Detailed performance insights
- Real-time alerts
- Analytics queries
- Works across multiple languages/frameworks
- Auto-sampling for high-traffic apps

**Cons** ❌
- Cost per GB ingested (~$2.50-$5)
- Learning curve for advanced queries
- Sampling can lose data
- Performance overhead (minimal)
- Integration required

### Limits & Constraints

| Resource | Limit |
|----------|-------|
| Data retention | 30-730 days |
| Daily ingestion | 100GB+ (configurable cap) |
| Query timeout | 10 minutes |
| Alert conditions | Unlimited |

### Real-World Use Cases

#### Use Case 1: Production Issue Diagnosis
```
Alert: Website response time > 5 seconds

DevOps investigates:
    └─ Application Insights
    ├── Performance timeline
    ├── Slow database query identified
    ├── Query time: 4 seconds (90% of response time)
    ├── Database CPU at 95%
    └── DBA adds index, resolves issue

Result: Identified root cause in 5 minutes
```

#### Use Case 2: User Experience Monitoring
```
Tracking:
├── Page load time (target: < 2 seconds)
├── User conversion funnel
├── Feature usage patterns
├── Error rates per page

Insights:
├── Checkout page slow (3 seconds)
├── 30% users abandon at checkout
├── Mobile users affected more
└── After optimization: 50% fewer abandons
```

### Performance Tips
- Enable client-side tracking
- Add custom events for business metrics
- Use sampling for high-volume apps
- Implement dependency tracking
- Set up alerts for critical issues
- Query logs regularly for insights

### Cost Optimization
- Enable sampling for high-volume data
- Archive old data to blob storage
- Set daily ingestion cap
- Remove verbose logging
- Use Log Analytics for long-term retention

---

## 3. Log Analytics

### What is it?
Centralized log aggregation and analysis service. Query and visualize logs from all your resources.

### When to Use
- Aggregate logs from multiple sources
- Security and compliance logging
- Long-term log retention
- Complex log queries
- Custom dashboards

### Key Features
- **Data Collection**: From VMs, containers, applications
- **Kusto Query Language (KQL)**: Powerful log queries
- **Workspaces**: Isolated log storage
- **Alerts**: Alert on log patterns
- **Update Management**: Track VM patches

### Pros & Cons

**Pros** ✅
- Flexible query language (KQL)
- Integrates with security tools
- Long retention (up to 2 years)
- Cost-effective for logs
- Works across cloud and on-premises

**Cons** ❌
- Query learning curve
- Complex setup
- Cost can grow with data volume
- Performance queries can be slow
- Retention decisions complex

### Real-World Use Cases

#### Use Case 1: Security Event Investigation
```
Alert: Suspicious login pattern

DevOps investigates:
    └─ Log Analytics
    ├── Query: Failed login attempts from country X
    ├── Result: 1000 failed attempts in 1 hour
    ├── Source IPs: All from same datacenter
    └── Action: Block IP range, reset user password

Result: Prevented unauthorized access
```

---

## 4. Azure Monitor

### What is it?
Infrastructure monitoring service that tracks metrics and logs from Azure resources.

### When to Use
- Monitor VM/resource metrics
- CPU, memory, disk usage
- Network metrics
- Uptime monitoring
- Infrastructure alerts

### Key Features
- **Metrics**: CPU, memory, disk, network
- **Alerts**: Alert on metric thresholds
- **Dashboards**: Visualize metrics
- **Auto-scale**: Scale resources based on metrics

### Real-World Use Cases

#### Use Case 1: Auto-Scale Application
```
Monitoring:
├── App Service CPU > 80% → Scale up (add instances)
├── App Service CPU < 20% → Scale down (remove instances)
└── Minimum 2 instances, maximum 10 instances

Benefits:
├── Cost optimization (scale down when quiet)
└── Performance (scale up when busy)
```

---

## Architecture Patterns

### Pattern 1: Complete Observability
```
Application
    ├── Application Insights (APM)
    │   ├── Request metrics
    │   ├── Exceptions
    │   ├── Dependencies
    │   └── Custom events
    │
    ├── Log Analytics (Logs)
    │   ├── Application logs
    │   ├── Infrastructure logs
    │   └── Security audit logs
    │
    └── Azure Monitor (Infrastructure)
        ├── CPU, memory, disk
        ├── Network metrics
        └── Auto-scaling rules

Dashboards:
    ├── Executive: Business metrics
    ├── DevOps: Infrastructure health
    ├── Dev: Application performance
    └── Security: Security events
```

### Pattern 2: Incident Response
```
Alert: Website down
    ↓
1. Check Azure Monitor (infrastructure healthy)
2. Check Application Insights (exceptions?)
    └─ Yes: 1000 errors per second
3. Check error details
    └─ Database connection timeout
4. Check Log Analytics (database logs)
    └─ Database at 99% CPU
5. Check database query logs
    └─ One slow query identified
6. Kill slow query, database CPU drops
7. Errors decrease, website recovers

Timeline: 10 minutes from alert to resolution
```

---

## Interview Questions

1. **Design CI/CD pipeline for .NET application**
   - Trigger on code commit
   - Build and unit test stage
   - Quality gate (SonarQube)
   - Deploy to staging
   - Integration tests
   - Manual approval
   - Deploy to production
   - Health checks and rollback

2. **Monitor production application**
   - Application Insights for APM
   - Log Analytics for logs
   - Azure Monitor for infrastructure
   - Custom dashboards per role
   - Alerts for critical issues

3. **Troubleshoot production incident**
   - Check Application Insights (exceptions?)
   - Check Azure Monitor (infrastructure healthy?)
   - Check Log Analytics (find root cause)
   - Check timeline (when did it start?)
   - Implement fix and redeploy
   - Monitor for regression

4. **Optimize application performance**
   - Profile with Application Insights
   - Find slowest operations
   - Cache frequently accessed data
   - Optimize database queries
   - Reduce API calls
   - Monitor improvements

5. **Secure pipeline and deployments**
   - Require code review before merge
   - Quality gates before production
   - Secret management (Key Vault)
   - Artifact signing
   - Approval gates
   - Audit trail of deployments

---

## Cost Optimization Tips

1. **Application Insights**: ~$2.50/GB, sample high-volume data
2. **Log Analytics**: ~$5/GB, archive old data
3. **Azure Monitor**: Low cost (metric-based)
4. **Pipelines**: Free tier (1800 min/month)
5. **Artifacts**: Clean up old artifacts regularly

---

**Last Updated**: April 2026  
**Difficulty**: Intermediate to Advanced  
**Focus**: CI/CD automation, monitoring, observability
