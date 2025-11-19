# DocuSign Webhook API - Unit Tests

This directory contains comprehensive unit tests for the DocuSign Webhook API application.

## Test Structure

The test project is organized to mirror the source code structure:

```
tests/DocuSignWebhook.Tests/
├── Controllers/
│   ├── DocuSignWebhookControllerTests.cs
│   └── EnvelopesControllerTests.cs
├── Services/
│   ├── WebhookProcessorTests.cs
│   ├── DocuSignServiceTests.cs
│   └── MinioStorageServiceTests.cs
├── Domain/
│   ├── BaseEntityTests.cs
│   ├── EnvelopeTests.cs
│   ├── DocumentTests.cs
│   └── WebhookEventTests.cs
└── DocuSignWebhook.Tests.csproj
```

## Test Coverage

The test suite aims for 100% code coverage across all testable components:

- **Domain Entities**: Full property validation and collection tests
- **Services**: Comprehensive testing with mocked dependencies
- **Controllers**: Full endpoint testing with various scenarios
- **Integration**: Database context and async operations

## Running Tests

### From Command Line

Run all tests:
```bash
dotnet test
```

Run tests with detailed output:
```bash
dotnet test --verbosity normal
```

Run tests for a specific test file:
```bash
dotnet test --filter "FullyQualifiedName~DocuSignWebhookControllerTests"
```

### Running Tests with Coverage

#### Linux/macOS:
```bash
./run-tests.sh
```

#### Windows:
```powershell
.\run-tests.ps1
```

### Manual Coverage Collection

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Test Technologies

- **xUnit**: Test framework
- **Moq**: Mocking framework for dependencies
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing
- **Coverlet**: Code coverage collection
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing support

## Test Categories

### 1. Domain Entity Tests
Tests for all domain entities ensuring:
- Default values are set correctly
- Properties can be set and retrieved
- Collections are initialized
- Nullable properties work as expected

### 2. Service Tests
Tests for application and infrastructure services:
- **WebhookProcessor**: HMAC validation, webhook processing, document handling
- **DocuSignService**: API client initialization
- **MinioStorageService**: Storage client initialization

### 3. Controller Tests
Tests for API controllers:
- **DocuSignWebhookController**: Webhook reception, signature validation, event processing
- **EnvelopesController**: Envelope retrieval, document listing, pagination

## Coverage Goals

Target: **100% Code Coverage**

The test suite covers:
- ✅ All public methods
- ✅ All controller actions
- ✅ All service methods
- ✅ All entity properties
- ✅ Error handling paths
- ✅ Edge cases and boundary conditions

## Continuous Integration

These tests are designed to run in CI/CD pipelines. The test project includes:
- Fast execution times
- No external dependencies (uses mocks and in-memory databases)
- Deterministic results
- Clear failure messages

## Adding New Tests

When adding new functionality:

1. Create a new test file matching the source file name with `Tests` suffix
2. Follow the existing test patterns (Arrange-Act-Assert)
3. Use descriptive test names: `MethodName_Scenario_ExpectedResult`
4. Mock all external dependencies
5. Test both success and failure paths
6. Run coverage to ensure new code is tested

## Example Test Pattern

```csharp
[Fact]
public async Task MethodName_WithValidInput_ReturnsExpectedResult()
{
    // Arrange
    var mockService = new Mock<IService>();
    mockService.Setup(x => x.Method()).ReturnsAsync(expectedValue);
    var controller = new Controller(mockService.Object);

    // Act
    var result = await controller.Action();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.Equal(expectedValue, okResult.Value);
}
```

## Notes

- Tests use in-memory Entity Framework contexts to avoid database dependencies
- All async operations are properly tested with async/await patterns
- Mocking is used extensively to isolate units under test
- Test data is kept minimal and focused on the scenario being tested
