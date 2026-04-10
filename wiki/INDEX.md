# Azure101 Wiki - Complete Azure Services Reference

Welcome to the Azure101 Wiki! This comprehensive reference covers all major Azure services with in-depth explanations, use cases, comparisons, and interview preparation materials.

## 📚 Table of Contents

### Service Categories

#### 1. **Compute Services**
Hosting and running applications from full-control VMs to serverless functions.
- [Compute Wiki](services/Compute.md) - Complete guide to all compute options
- VMs, App Service, Functions, Containers, Kubernetes, Batch

#### 2. **Storage Services**
Data storage from unstructured blobs to NoSQL tables.
- [Storage Wiki](services/Storage.md) - Complete guide to all storage options
- Blob Storage, File Shares, Queue Storage, Table Storage, Data Lake

#### 3. **Networking Services**
Network infrastructure and connectivity solutions.
- [Networking Wiki](services/Networking.md) - Complete guide to networking
- VNets, Load Balancers, App Gateway, CDN, Firewall, ExpressRoute

#### 4. **Database Services**
Relational and NoSQL databases, caching, and data integration.
- [Databases Wiki](services/Databases.md) - Complete guide to databases
- SQL Database, Cosmos DB, PostgreSQL, MySQL, Redis, Data Factory

#### 5. **Messaging & Events**
Asynchronous communication and event processing.
- [Messaging Wiki](services/Messaging.md) - Complete guide to messaging
- Service Bus, Event Hubs, Event Grid, Notification Hubs

#### 6. **Security & Identity**
Authentication, authorization, secrets, and compliance.
- [Security Wiki](services/Security.md) - Complete guide to security
- Key Vault, App Configuration, Managed Identity, RBAC, AAD

#### 7. **AI & Machine Learning**
Cognitive services, machine learning, and AI models.
- [AI/ML Wiki](services/AI_ML.md) - Complete guide to AI services
- Cognitive Services, Azure OpenAI, ML, Bot Service, Form Recognizer

#### 8. **DevOps & Monitoring**
Application monitoring, logging, CI/CD, and operations.
- [DevOps Wiki](services/DevOps.md) - Complete guide to DevOps
- App Insights, Log Analytics, Azure Pipelines, DevTest Labs

#### 9. **Integration Services**
API management, serverless functions, and enterprise integration.
- [Integration Wiki](services/Integration.md) - Complete guide to integration
- API Management, Functions, Service Bus, Logic Apps

#### 10. **Data & Analytics** (Coming Soon)
Big data processing and analytics services.
- Data Lake Storage, Synapse, Databricks, Stream Analytics, Power BI

---

## 🎓 FAQ Sections

### [Most Common FAQ](../faq/MOST_COMMON_FAQ.md)
Top 50 frequently asked questions covering:
- Service selection (when to use each)
- Basic operations and setup
- Common real-world scenarios
- Cost and optimization
- Security best practices
- Authentication and authorization

### [Complex & Advanced FAQ](../faq/COMPLEX_FAQ.md)
Advanced topics for architects and senior developers:
- Multi-region and disaster recovery
- Complex scaling scenarios
- Security hardening
- Performance optimization
- Cost optimization at scale
- Enterprise patterns
- Hybrid and multi-cloud

### [Interview Preparation FAQ](../faq/INTERVIEW_FAQ.md)
Curated questions for technical interviews:
- Service comparisons
- Architectural decisions
- Trade-offs and limitations
- Real-world scenarios
- Coding and implementation
- Scenario-based questions

---

## 🔍 Quick Service Comparison

### By Use Case

**Web Applications & APIs**
- App Service (most common, PaaS)
- Functions (serverless, event-driven)
- API Management (API gateway)

**Data Storage**
- Blob Storage (unstructured data)
- SQL Database (relational, OLTP)
- Cosmos DB (globally distributed, flexible schema)
- Data Lake Storage (big data analytics)

**High-Performance Computing**
- Virtual Machines (full control, scale-out)
- Batch (parallel processing)
- Kubernetes (container orchestration)

**Real-Time Messaging**
- Service Bus (reliable queuing, topics)
- Event Hubs (streaming, telemetry)
- Event Grid (event routing)

**Monitoring & Diagnostics**
- Application Insights (app monitoring)
- Log Analytics (log aggregation)
- Azure Monitor (infrastructure metrics)

**Security**
- Key Vault (secrets management)
- Managed Identity (passwordless auth)
- Azure Security Center (threat detection)

---

## 📖 Learning Paths

### Path 1: Web Developer (Building Web Apps)
1. [App Service Wiki](services/Compute.md#app-service) - Hosting
2. [SQL Database Wiki](services/Databases.md#sql-database) - Data
3. [Storage Wiki](services/Storage.md#blob-storage) - Files/Assets
4. [Authentication](services/Security.md#authentication) - User Identity
5. [DevOps & Monitoring](services/DevOps.md) - Monitoring

### Path 2: Solution Architect (Designing Systems)
1. Compute Services (all options)
2. Networking Services (connectivity)
3. Database Services (data patterns)
4. Messaging Services (async patterns)
5. Security Services (compliance)
6. Disaster Recovery & Multi-Region

### Path 3: Data Engineer (Building Data Pipelines)
1. [Storage Wiki](services/Storage.md) - Data Lake Storage
2. [Databases Wiki](services/Databases.md) - Cosmos DB, SQL
3. [Messaging Wiki](services/Messaging.md) - Event Hubs
4. [DevOps Wiki](services/DevOps.md) - Monitoring
5. Data Factory & Synapse (coming soon)

### Path 4: DevOps Engineer (Infrastructure & Operations)
1. [Compute Wiki](services/Compute.md) - All compute
2. [Networking Wiki](services/Networking.md) - Network infrastructure
3. [Security Wiki](services/Security.md) - Security hardening
4. [DevOps Wiki](services/DevOps.md) - Monitoring and logging
5. Infrastructure as Code patterns

---

## 🔧 How to Use This Wiki

### For Learning
1. **Start with Overview** - Read the service overview for breadth
2. **Dive into Details** - Explore core concepts and architecture
3. **Study Examples** - Review code examples and patterns
4. **Practice** - Build sample applications using the service
5. **Review FAQ** - Answer common questions about the service

### For Interview Preparation
1. **Study Service Comparisons** - Understand when to use each
2. **Learn Trade-offs** - Know pros, cons, and limitations
3. **Review Complex FAQ** - Understand advanced scenarios
4. **Practice Scenarios** - Be ready to design solutions
5. **Code Samples** - Be able to implement basic operations

### For Architecture Decisions
1. **Use Comparison Tables** - Quick service comparison
2. **Review Use Cases** - Understand real-world scenarios
3. **Check Complex FAQ** - Handle edge cases
4. **Reference Patterns** - Use proven architectural patterns
5. **Cost Analysis** - Understand pricing implications

---

## 📊 Service Cheat Sheet

| Service | Type | Capacity | Latency | Cost Model |
|---------|------|----------|---------|-----------|
| Virtual Machines | IaaS | Massive | Low | Per minute |
| App Service | PaaS | High | Low | Per tier |
| Functions | Serverless | Unlimited | Medium | Per execution |
| Blob Storage | Storage | Massive | Low | Per GB |
| SQL Database | Database | High | Low | Per DTU/vCore |
| Cosmos DB | NoSQL | Massive | Ultra-low | Per RU |
| Service Bus | Messaging | High | Low | Per message |
| Event Hubs | Streaming | Massive | Low | Per throughput unit |
| Key Vault | Security | Limited | Low | Per operation |
| App Insights | Monitoring | Massive | Low | Per GB ingested |

---

## 🌟 Key Concepts Glossary

- **IaaS (Infrastructure as a Service)** - Full control, you manage everything
- **PaaS (Platform as a Service)** - Middle ground, platform handles some concerns
- **SaaS (Software as a Service)** - Complete solution, minimal management
- **Serverless** - Code runs without provisioning or managing servers
- **Scalability** - Ability to handle increasing load
- **Availability** - System uptime and reliability
- **Redundancy** - Duplicate resources for fault tolerance
- **RTO (Recovery Time Objective)** - How quickly you can recover from failure
- **RPO (Recovery Point Objective)** - How much data loss is acceptable
- **SLA (Service Level Agreement)** - Uptime guarantee (e.g., 99.9%)

---

## 🚀 Getting Started

1. **Start Here**: Read your role's learning path above
2. **Deep Dive**: Explore relevant service wiki pages
3. **FAQ Review**: Check Common FAQ for quick answers
4. **Code Examples**: Review code samples in src/ directories
5. **Interview Prep**: Study Interview FAQ for technical interviews

---

## 📝 Notes on This Wiki

- **Accuracy**: Information current as of April 2026
- **Completeness**: All major services covered, with expansion underway
- **Practical Focus**: Emphasis on real-world usage and patterns
- **Interview Ready**: Content designed for technical interview preparation
- **Continuously Updated**: Check back regularly for new content

---

## 🔗 Related Resources

- [Main README.md](../README.md) - Project overview
- [Getting Started Guide](../GETTING_STARTED.md) - Setup instructions
- [Code Examples](../src/) - Working C# examples
- [Contributing Guide](../CONTRIBUTING.md) - How to contribute

---

**Last Updated**: April 2026  
**Total Wiki Pages**: 10+ service guides  
**Total FAQ Questions**: 100+  
**Code Examples**: Expanding  
**Status**: 🟢 Active Development
