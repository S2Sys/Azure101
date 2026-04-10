# Getting Started with Azure101

This guide will help you set up and start using Azure101 for learning and interview preparation.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required
- **.NET 8.0 or later** - [Download .NET](https://dotnet.microsoft.com/download)
- **Git** - [Download Git](https://git-scm.com/downloads)
- **Text Editor or IDE** (pick one):
  - Visual Studio 2022 Community Edition (Recommended for Windows)
  - Visual Studio Code (Cross-platform)
  - JetBrains Rider (Cross-platform, paid)

### Optional
- **Azure Subscription** - For testing against real Azure services
  - Free tier available at [azure.microsoft.com/free](https://azure.microsoft.com/free)
- **Azure CLI** - For managing Azure resources
  - [Download Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- **Azure Storage Emulator** - For local Azure Storage testing
  - [Azurite](https://github.com/Azure/Azurite)

## Installation Steps

### 1. Clone the Repository

```bash
git clone https://github.com/S2Sys/Azure101.git
cd Azure101
```

### 2. Verify .NET Installation

```bash
dotnet --version
# Should output: 8.0.x or higher
```

### 3. Restore NuGet Packages

```bash
dotnet restore
```

This downloads all required dependencies specified in the project files.

### 4. Build the Solution

```bash
dotnet build
```

If the build succeeds, all projects should compile without errors.

### 5. Run the Tests (Optional)

```bash
dotnet test
```

This verifies that all example code works correctly.

## Project Structure Overview

### Source Projects (`src/`)

Each project contains:
- **Examples/** - Working code samples
- **InterviewContent/** - Interview questions and answers
- **Models/** - Data models specific to that service

```
src/
├── Azure101.Core/              # Shared utilities
├── Azure101.Compute/           # Virtual Machines, App Service, etc.
├── Azure101.Storage/           # Blob Storage, File Shares, etc.
├── Azure101.Networking/        # Virtual Networks, Load Balancers, etc.
├── Azure101.Databases/         # SQL, Cosmos DB, etc.
├── Azure101.Messaging/         # Service Bus, Event Hubs, etc.
├── Azure101.Security/          # Key Vault, Managed Identity, etc.
├── Azure101.AI_ML/             # Cognitive Services, OpenAI, etc.
├── Azure101.DevOps/            # App Insights, Log Analytics, etc.
├── Azure101.Integration/       # API Management, Functions, etc.
└── Azure101.Documentation/     # Interview Q&A models & helpers
```

### Test Projects (`tests/`)

Parallel test project structure with Unit and Integration test folders:

```
tests/
├── Azure101.Core.Tests/
├── Azure101.Compute.Tests/
├── Azure101.Storage.Tests/
└── ... (one for each service)
```

### Documentation (`docs/`)

```
docs/
├── services/                   # Service-specific guides
│   ├── Compute.md
│   ├── Storage.md
│   └── ...
├── interview-prep/             # Interview preparation
│   ├── INTERVIEW_GUIDE.md
│   └── COMMON_QUESTIONS.md
└── ARCHITECTURE.md
```

## How to Use Azure101

### For Learning Azure Services

1. **Pick a Service** - Choose an Azure service you want to learn
   - Example: Azure Storage Blob Service

2. **Read the Overview** - Start with the service guide
   - Location: `docs/services/Storage.md`
   - Learn: What it is, when to use it, key concepts

3. **Review Code Examples** - Understand practical implementation
   - Location: `src/Azure101.Storage/Examples/`
   - Study: How to authenticate, perform operations, handle errors

4. **Run the Examples** - Execute code in your IDE or terminal
   - Try modifying examples to experiment
   - Check tests to see edge cases

5. **Deep Dive into Patterns** - Learn real-world architectural patterns
   - Location: Service guide's "Real-World Patterns" section
   - Understand: When and why to use this service

### For Interview Preparation

1. **Review Service Categories** - Understand all major Azure services
   - Spend time on: Compute, Storage, Databases, Networking, Security

2. **Study Interview Questions** - Practice common questions
   - Location: Each service project's `InterviewContent/InterviewQA.cs`
   - Focus on: Pros/cons, when to use, architectural decisions

3. **Practice Code Examples** - Be ready to code during interviews
   - Review: Authentication, CRUD operations, error handling
   - Can you explain: How this service works, why it's needed?

4. **Understand Comparisons** - Know when to choose one service over another
   - Example: Blob Storage vs. Files vs. Data Lake Storage
   - Study: Pros/cons comparison tables in service guides

5. **Learn Architectural Patterns** - Understand real-world scenarios
   - Example: When to use Service Bus vs. Event Hubs vs. Event Grid
   - Practice: Designing solutions using multiple services

## Authentication Setup

### For Local Development

Azure101 uses **DefaultAzureCredential**, which tries authentication methods in order:

1. Environment Variables (AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET)
2. Managed Identity (if running in Azure)
3. Visual Studio credentials (if logged in)
4. Azure CLI credentials
5. PowerShell credentials

**Easiest Option: Azure CLI**

```bash
# Install Azure CLI
# https://docs.microsoft.com/cli/azure/install-azure-cli

# Login
az login

# Verify
az account show
```

Now your code can use `DefaultAzureCredential()` without any configuration!

### For Production

Never hardcode credentials. Use one of:
- **Managed Identity** (if running in Azure)
- **Key Vault** (store secrets securely)
- **Service Principal** (for CI/CD)
- **Azure AD** (for user authentication)

See `src/Azure101.Core/Authentication/AuthenticationHelper.cs` for examples.

## Running Code Examples

### From Visual Studio

1. Open `Azure101.sln` in Visual Studio
2. Navigate to desired example file (e.g., `src/Azure101.Storage/Examples/BlobExample.cs`)
3. Set as startup project if it's a console app
4. Press F5 or use "Debug" → "Start Debugging"

### From Command Line

```bash
# Build specific project
dotnet build src/Azure101.Storage

# Run tests for specific project
dotnet test tests/Azure101.Storage.Tests

# Run all tests with verbose output
dotnet test --verbosity detailed
```

### Running Individual Test Methods

```bash
# Run specific test
dotnet test --filter "FullyQualifiedName~BlobUploadTest"

# Run with specific verbosity
dotnet test --verbosity detailed

# Run with code coverage
dotnet test /p:CollectCoverageMetrics=true
```

## Setting Up Azure Resources (Optional)

To test against real Azure services, you'll need resources:

### Create Azure Resources

```bash
# Login to Azure
az login

# Create a resource group
az group create --name azure101-rg --location eastus

# Create a storage account
az storage account create \
  --name <unique-storage-name> \
  --resource-group azure101-rg \
  --location eastus

# Get connection string (use in examples)
az storage account show-connection-string \
  --name <unique-storage-name> \
  --resource-group azure101-rg
```

### Set Environment Variables

```bash
# Set connection string as environment variable
export AZURE_STORAGE_CONNECTION_STRING="<connection-string>"

# Or set credentials for other services
export AZURE_TENANT_ID="<your-tenant-id>"
export AZURE_CLIENT_ID="<your-client-id>"
export AZURE_CLIENT_SECRET="<your-client-secret>"
```

## Common Commands

```bash
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Azure101.Storage.Tests

# Clean build
dotnet clean

# List projects
dotnet sln list

# Add new project to solution
dotnet sln add src/NewProject/NewProject.csproj

# Format code
dotnet format
```

## Troubleshooting

### "Azure SDK not found" or NuGet errors

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

### "Cannot authenticate"

1. Verify Azure CLI is installed: `az --version`
2. Login to Azure: `az login`
3. Check logged-in account: `az account show`
4. If still failing, try explicit credentials in code using `ClientSecretCredential`

### Build errors related to .NET version

```bash
# Check your .NET version
dotnet --version

# You need .NET 8.0 or later
# Download from: https://dotnet.microsoft.com/download
```

### Tests failing

1. Check you have internet access (needed for some Azure operations)
2. Check authentication is set up (see Authentication Setup section)
3. Run with verbose output: `dotnet test --verbosity detailed`
4. Check test output for specific error message

## Next Steps

1. **Choose a Service** - Pick one that interests you
   - Start with familiar services like Storage or Databases
   - Or start with fundamental ones like Security & Authentication

2. **Read the Service Guide** - Understand concepts
   - `docs/services/<ServiceName>.md`

3. **Study Code Examples** - See practical implementation
   - `src/Azure101.<ServiceName>/Examples/`

4. **Review Interview Questions** - Prepare for interviews
   - `src/Azure101.<ServiceName>/InterviewContent/`

5. **Practice & Experiment**
   - Modify examples to experiment
   - Try different approaches
   - Break things, fix them, learn!

6. **Build Something** - Apply what you learned
   - Create a small project using Azure services
   - Combine multiple services
   - Deploy to Azure

## Additional Resources

### Official Microsoft Documentation
- [Azure Documentation](https://docs.microsoft.com/azure/)
- [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net)
- [Azure Architecture Center](https://docs.microsoft.com/azure/architecture)

### Learning Paths
- [Azure Fundamentals (Microsoft Learn)](https://learn.microsoft.com/azure/fundamentals/)
- [Azure Developer Associate Certification](https://learn.microsoft.com/certifications/azure-developer-associate/)
- [Azure Solutions Architect Expert](https://learn.microsoft.com/certifications/azure-solutions-architect-expert/)

### Community & Support
- [Stack Overflow - azure tag](https://stackoverflow.com/questions/tagged/azure)
- [GitHub Discussions](https://github.com/S2Sys/Azure101/discussions)
- [Microsoft Azure Forums](https://social.msdn.microsoft.com/Forums/azure/en-US/home)

## Tips for Success

1. **Practice Consistently** - Spend time with code examples regularly
2. **Build Projects** - Apply knowledge by building real projects
3. **Read Documentation** - Understand the "why" behind design choices
4. **Ask Questions** - Don't hesitate to explore and experiment
5. **Join Communities** - Learn from others' experiences
6. **Stay Updated** - Azure evolves, keep learning new features
7. **Take Notes** - Write down key concepts and patterns
8. **Teach Others** - Explain concepts to solidify understanding

---

**Need Help?** Check the [README.md](README.md) or GitHub Issues for common questions.
