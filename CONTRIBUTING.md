# Contributing to Azure101

We welcome contributions! This guide explains how to contribute to Azure101.

## Code of Conduct

Be respectful and constructive in all interactions.

## How to Contribute

### Reporting Issues

1. Check if the issue already exists
2. Provide clear description
3. Include steps to reproduce (if applicable)
4. Include environment details (.NET version, OS, etc.)

### Adding Content

#### Adding Code Examples

1. **Create example class** in `src/Azure101.<Service>/Examples/`
   ```csharp
   public class BlobStorageExample
   {
       public async Task BasicOperationsAsync()
       {
           // Clear, well-commented example code
       }
   }
   ```

2. **Include comments** explaining key concepts
3. **Use async/await** throughout
4. **Handle errors gracefully**
5. **Add corresponding unit tests**
6. **Document in service guide**

#### Adding Interview Questions

1. **Edit** `src/Azure101.<Service>/InterviewContent/InterviewQA.cs`
2. **Add question** with:
   - Clear question text
   - Comprehensive answer
   - Detailed explanation if needed
   - Appropriate difficulty level
   - Related topics
3. **Review** for accuracy
4. **Test** that code examples compile

#### Adding Documentation

1. **Update** relevant service guide in `docs/services/`
2. **Follow** existing format and style
3. **Include code snippets** that reference examples
4. **Add diagrams** where helpful
5. **Review** for clarity and accuracy

### Pull Request Process

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/description`
3. **Make** your changes
4. **Add** tests if applicable
5. **Test** your changes: `dotnet test`
6. **Commit** with clear messages: `git commit -m "Add feature X for service Y"`
7. **Push** to your fork
8. **Create** a Pull Request with:
   - Clear title describing the change
   - Description of what was added/fixed
   - Why the change was needed
   - Any relevant issues (#123)

### Commit Message Guidelines

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types:
- `feat`: New feature (example, documentation, etc.)
- `fix`: Bug fix
- `docs`: Documentation update
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build, dependencies, etc.

Examples:
```
feat(storage): Add blob upload example
fix(core): Handle null reference in retry policy
docs(databases): Update SQL Database guide
test(messaging): Add Service Bus integration tests
```

## Coding Standards

### C# Guidelines

- **Naming**: PascalCase for public members, camelCase for private
- **Async**: Use `async/await`, avoid `.Result` or `.Wait()`
- **Null Safety**: Enable `#nullable enable` at file top
- **Logging**: Use injected `ILogger<T>`
- **Comments**: Explain "why", not "what"
- **Example**:

```csharp
using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Azure101.Storage.Examples;

public class BlobStorageExample
{
    private readonly ILogger<BlobStorageExample> _logger;
    private readonly BlobContainerClient _container;

    public BlobStorageExample(
        ILogger<BlobStorageExample> logger,
        BlobContainerClient container)
    {
        _logger = logger;
        _container = container;
    }

    /// <summary>
    /// Uploads a blob using the provided content
    /// </summary>
    public async Task UploadBlobAsync(string blobName, string content)
    {
        _logger.LogInformation("Uploading blob: {BlobName}", blobName);

        try
        {
            var blobClient = _container.GetBlobClient(blobName);
            await blobClient.UploadAsync(
                BinaryData.FromString(content),
                overwrite: true);

            _logger.LogInformation("Blob uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob");
            throw;
        }
    }
}
```

### Documentation Guidelines

- **Headers**: Clear hierarchy (H1 for main title, H2 for sections, etc.)
- **Code Blocks**: Language-specific syntax highlighting
- **Examples**: Real, runnable code where possible
- **Links**: Absolute URLs to public resources
- **TOC**: Add table of contents for long documents

## Testing Guidelines

### Unit Tests

```csharp
[TestClass]
public class BlobStorageExampleTests
{
    private Mock<BlobContainerClient> _mockContainer;
    private BlobStorageExample _example;

    [TestInitialize]
    public void Setup()
    {
        _mockContainer = new Mock<BlobContainerClient>();
        _example = new BlobStorageExample(
            Mock.Of<ILogger<BlobStorageExample>>(),
            _mockContainer.Object);
    }

    [TestMethod]
    public async Task UploadBlobAsync_WithValidContent_UploadsSuccessfully()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        _mockContainer
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        await _example.UploadBlobAsync("test-blob", "test content");

        // Assert
        mockBlobClient.Verify(
            c => c.UploadAsync(It.IsAny<BinaryData>(), true),
            Times.Once);
    }
}
```

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/Azure101.Storage.Tests

# Specific method
dotnet test --filter "UploadBlobAsync_WithValidContent_UploadsSuccessfully"

# With coverage
dotnet test /p:CollectCoverageMetrics=true
```

## Documentation Structure

### Service Guide Template (`docs/services/<Service>.md`)

```markdown
# Azure <Service Name>

## Overview
- What is it?
- Key use cases
- When to use

## Core Concepts
- Concept 1
- Concept 2

## Pros and Cons
### Pros
- Advantage 1
- Advantage 2

### Cons
- Limitation 1
- Limitation 2

## Code Examples
[Link to examples]

## Real-World Patterns
- Pattern 1: Description
- Pattern 2: Description

## Performance Tips
- Tip 1
- Tip 2

## Common Interview Questions
1. Question 1?
   - Answer 1
2. Question 2?
   - Answer 2
```

## Review Process

1. **Automated Checks**: Tests and build must pass
2. **Code Review**: At least one approval required
3. **Documentation**: Updates must be included
4. **Quality**: Code should follow existing standards

## Becoming a Maintainer

Active contributors who consistently provide high-quality work may be invited to become maintainers. This includes:
- Review and merge PRs
- Triage issues
- Guide community
- Make architectural decisions

## Questions?

- Check [GETTING_STARTED.md](GETTING_STARTED.md) for setup help
- Review existing examples and tests
- Open a GitHub Discussion
- Create an Issue for bugs/features

Thank you for contributing to Azure101! 🚀
