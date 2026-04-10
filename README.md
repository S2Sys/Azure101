# Azure101: Comprehensive C# Azure Services Guide

A comprehensive learning resource and interview preparation guide for Azure services, built with C#. This project serves as both an educational reference and technical interview preparation material with practical code examples, real-world patterns, pros/cons analysis, and interview Q&A.

## 📚 Project Overview

**Azure101** is a monolithic Visual Studio solution covering all major Azure services with:
- **Practical Code Examples** - Real-world scenarios and patterns
- **Interview Q&A** - Common questions with detailed answers
- **Architectural Analysis** - Pros/cons, best practices, and performance tips
- **Unit & Integration Tests** - Verified code examples
- **Documentation** - Service guides and learning paths

## 🎯 Target Audience

- **Developers** learning Azure platform
- **Interview Candidates** preparing for Azure-related technical interviews
- **Solutions Architects** reviewing best practices and patterns
- **DevOps Engineers** implementing cloud infrastructure

## 📦 Project Structure

```
Azure101/
├── src/                                  # Source projects
│   ├── Azure101.Core/                   # Shared utilities & infrastructure
│   ├── Azure101.Compute/                # VMs, App Service, ACI, AKS, Functions
│   ├── Azure101.Storage/                # Blob, Files, Queues, Tables, ADLS
│   ├── Azure101.Networking/             # VNets, LB, AppGateway, CDN, etc.
│   ├── Azure101.Databases/              # SQL, Cosmos DB, MySQL, PostgreSQL, Redis
│   ├── Azure101.Messaging/              # Service Bus, Event Hubs, Event Grid
│   ├── Azure101.Security/               # Key Vault, App Config, Managed Identity
│   ├── Azure101.AI_ML/                  # Cognitive Services, OpenAI, ML
│   ├── Azure101.DevOps/                 # App Insights, Log Analytics, Pipelines
│   ├── Azure101.Integration/            # API Management, Functions, Logic Apps
│   └── Azure101.Documentation/          # Interview Q&A and documentation content
│
├── tests/                               # Test projects (10)
│   ├── Azure101.*.Tests/
│   └── Unit/ & Integration/ folders
│
├── docs/                                # Documentation
│   ├── services/                        # Service guides
│   ├── interview-prep/                  # Interview preparation materials
│   └── ARCHITECTURE.md
│
└── examples/                            # Quick reference examples
```

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 or later
- Visual Studio 2022 / VS Code / JetBrains Rider
- Azure Subscription (optional, for real Azure service examples)
- Git

### Installation

```bash
# Clone the repository
git clone https://github.com/S2Sys/Azure101.git
cd Azure101

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

### Quick Start Example

```csharp
// Azure Storage Blob example
using Azure.Storage.Blobs;
using Azure.Identity;

var blobUri = "https://<account>.blob.core.windows.net/<container>";
var blobClient = new BlobClient(new Uri(blobUri), new DefaultAzureCredential());

// Upload a blob
await blobClient.UploadAsync(BinaryData.FromString("Hello, Azure!"), overwrite: true);

// Download a blob
BlobDownloadInfo download = await blobClient.DownloadAsync();
```

See `/examples` folder for more examples organized by service.

## 📖 Services Covered

### Core (Azure101.Core)
- Authentication helpers (DefaultAzureCredential, Managed Identity, etc.)
- Configuration management
- Retry policies with exponential backoff
- Logging setup
- Common DTOs and models

### Compute Services (Azure101.Compute)
- **Virtual Machines** - IaaS compute
- **App Service** - PaaS web/API hosting
- **Container Instances** - Serverless containers
- **Azure Kubernetes Service** - Orchestrated containers
- **Functions** - Event-driven serverless
- **Batch** - Large-scale parallel processing

### Storage Services (Azure101.Storage)
- **Blob Storage** - Unstructured data
- **File Shares** - SMB-based file storage
- **Queue Storage** - Message queuing
- **Table Storage** - NoSQL entity storage
- **Data Lake Storage** - Big data analytics

### Networking Services (Azure101.Networking)
- **Virtual Networks** - Network isolation & connectivity
- **Load Balancer** - Layer 4 load balancing
- **Application Gateway** - Layer 7 load balancing
- **Network Security Groups** - Network access control
- **Azure Firewall** - Centralized threat protection
- **VPN Gateway** - Site-to-site & point-to-site
- **ExpressRoute** - Dedicated private connectivity
- **CDN** - Content delivery network
- **Front Door** - Global load balancing & WAF

### Database Services (Azure101.Databases)
- **Azure SQL Database** - Relational, fully managed
- **Cosmos DB** - Global, multi-model NoSQL
- **Azure Database for MySQL/PostgreSQL/MariaDB** - Open source
- **Azure Cache for Redis** - In-memory data store
- **Data Factory** - Data integration & movement

### Messaging Services (Azure101.Messaging)
- **Service Bus** - Queues, Topics, Subscriptions
- **Event Hubs** - Stream processing & telemetry
- **Event Grid** - Event routing
- **Notification Hubs** - Push notifications

### Security Services (Azure101.Security)
- **Key Vault** - Secrets & certificate management
- **App Configuration** - Centralized configuration
- **Managed Identity** - Passwordless authentication
- **Role-Based Access Control (RBAC)**
- **Advanced Threat Protection**

### AI & ML Services (Azure101.AI_ML)
- **Cognitive Services** - Vision, Language, Speech, Decision
- **Azure OpenAI** - GPT models
- **Machine Learning** - Managed ML platform
- **Form Recognizer** - Document intelligence
- **Translator** - Language translation

### DevOps Services (Azure101.DevOps)
- **Application Insights** - Application monitoring
- **Log Analytics** - Log aggregation & analysis
- **Azure Pipelines** - CI/CD
- **DevTest Labs** - Development environments
- **Artifacts** - Package management

### Integration Services (Azure101.Integration)
- **API Management** - API gateway & management
- **Azure Functions** - Serverless functions
- **Service Bus** - Enterprise messaging
- **Event Grid** - Event-driven architecture

## 📚 Documentation Structure

Each service includes:

### Code Examples
- Basic CRUD operations
- Advanced scenarios and optimizations
- Authentication setup
- Error handling best practices
- Async/await patterns

### Interview Questions
- 10-15 questions per service
- Difficulty levels: Beginner, Intermediate, Advanced
- Detailed answers with explanations
- Related topics and tags

### Analysis Documentation
- Service overview and use cases
- Pros and cons comparison table
- Real-world architectural patterns
- Performance tips and best practices
- When to use each service

## 🧪 Testing

The project includes comprehensive test coverage:

```bash
# Run all tests
dotnet test

# Run tests for specific service
dotnet test tests/Azure101.Storage.Tests

# Run with code coverage
dotnet test /p:CollectCoverageMetrics=true
```

Test categories:
- **Unit Tests** - Mocked Azure SDK clients
- **Integration Tests** - Real Azure services (optional)
- **Example Tests** - Verify code examples compile

## 📝 Interview Preparation

Use this guide to prepare for Azure-related technical interviews:

1. **Study Service Categories** - Review docs/services/ for each Azure service
2. **Review Q&A** - Study interview questions in each project's InterviewContent
3. **Practice Code** - Review and run examples from each service
4. **Understand Patterns** - Learn real-world architectural patterns
5. **Compare Services** - Understand pros/cons and when to use each

See [INTERVIEW_GUIDE.md](docs/interview-prep/INTERVIEW_GUIDE.md) for interview tips and strategies.

## 🔧 Development Guidelines

### Adding a New Example

1. Create example class in `src/Azure101.ServiceName/Examples/`
2. Name it descriptively: `BlobStorageUploadExample.cs`
3. Include comments explaining key concepts
4. Add corresponding unit tests
5. Document in service guide

### Adding Interview Questions

1. Add to `src/Azure101.ServiceName/InterviewContent/InterviewQA.cs`
2. Include question, answer, detailed explanation
3. Set appropriate difficulty level
4. Add related topics and tags
5. Review for accuracy

### Code Style

- Follow C# naming conventions (PascalCase for public members)
- Use nullable reference types (`#nullable enable`)
- Async all the way (`async/await`)
- Comment complex logic
- Use dependency injection

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📞 Support & Feedback

- **GitHub Issues** - Report bugs or suggest features
- **Discussions** - Ask questions and share knowledge
- **Pull Requests** - Contribute improvements

## 🎓 Learning Resources

- [Microsoft Azure Documentation](https://docs.microsoft.com/azure)
- [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net)
- [Azure Architecture Center](https://docs.microsoft.com/azure/architecture)
- [Azure Well-Architected Framework](https://docs.microsoft.com/azure/architecture/framework)

## 📈 Project Roadmap

- [x] Project structure & core infrastructure
- [ ] Compute services examples & documentation
- [ ] Storage services examples & documentation
- [ ] Networking services examples & documentation
- [ ] Database services examples & documentation
- [ ] Messaging services examples & documentation
- [ ] Security services examples & documentation
- [ ] AI/ML services examples & documentation
- [ ] DevOps services examples & documentation
- [ ] Integration services examples & documentation
- [ ] Comprehensive interview Q&A index
- [ ] Architecture decision documentation

---

**Last Updated:** April 2026  
**Maintained by:** Azure101 Contributors
