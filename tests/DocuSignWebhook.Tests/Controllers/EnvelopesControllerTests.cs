using DocuSignWebhook.API.Controllers;
using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuSignWebhook.Tests.Controllers;

public class EnvelopesControllerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<EnvelopesController>> _mockLogger;
    private readonly EnvelopesController _controller;

    public EnvelopesControllerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockLogger = new Mock<ILogger<EnvelopesController>>();

        SetupMockDbSets();

        _controller = new EnvelopesController(
            _mockContext.Object,
            _mockLogger.Object);
    }

    private void SetupMockDbSets()
    {
        var envelopes = new List<Envelope>
        {
            new Envelope
            {
                Id = Guid.NewGuid(),
                DocuSignEnvelopeId = "env1",
                Subject = "Test 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Envelope
            {
                Id = Guid.NewGuid(),
                DocuSignEnvelopeId = "env2",
                Subject = "Test 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Envelope
            {
                Id = Guid.NewGuid(),
                DocuSignEnvelopeId = "env3",
                Subject = "Test 3 Inactive",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        }.AsQueryable();

        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        var documents = new List<Document>().AsQueryable();
        var mockDocumentSet = CreateMockDbSet(documents);
        _mockContext.Setup(c => c.Documents).Returns(mockDocumentSet.Object);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mockSet;
    }

    [Fact]
    public async Task GetEnvelopes_ReturnsActiveEnvelopes()
    {
        // Act
        var result = await _controller.GetEnvelopes();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelopes = Assert.IsAssignableFrom<IEnumerable<Envelope>>(okResult.Value);
        Assert.Equal(2, envelopes.Count());
    }

    [Fact]
    public async Task GetEnvelopes_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var result = await _controller.GetEnvelopes(skip: 1, take: 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelopes = Assert.IsAssignableFrom<IEnumerable<Envelope>>(okResult.Value);
        Assert.Single(envelopes);
    }

    [Fact]
    public async Task GetEnvelope_WithExistingId_ReturnsEnvelope()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.NewGuid(),
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            IsActive = true,
            Documents = new List<Document>(),
            WebhookEvents = new List<WebhookEvent>()
        };

        var envelopes = new List<Envelope> { envelope }.AsQueryable();
        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        // Act
        var result = await _controller.GetEnvelope(envelope.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedEnvelope = Assert.IsType<Envelope>(okResult.Value);
        Assert.Equal(envelope.Id, returnedEnvelope.Id);
    }

    [Fact]
    public async Task GetEnvelope_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _controller.GetEnvelope(nonExistentId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEnvelope_WithInactiveEnvelope_ReturnsNotFound()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.NewGuid(),
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            IsActive = false
        };

        var envelopes = new List<Envelope> { envelope }.AsQueryable();
        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        // Act
        var result = await _controller.GetEnvelope(envelope.Id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEnvelopeByDocuSignId_WithExistingId_ReturnsEnvelope()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.NewGuid(),
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            IsActive = true,
            Documents = new List<Document>(),
            WebhookEvents = new List<WebhookEvent>()
        };

        var envelopes = new List<Envelope> { envelope }.AsQueryable();
        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        // Act
        var result = await _controller.GetEnvelopeByDocuSignId("env123");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedEnvelope = Assert.IsType<Envelope>(okResult.Value);
        Assert.Equal("env123", returnedEnvelope.DocuSignEnvelopeId);
    }

    [Fact]
    public async Task GetEnvelopeByDocuSignId_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetEnvelopeByDocuSignId("nonexistent");

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetEnvelopeDocuments_WithExistingEnvelope_ReturnsDocuments()
    {
        // Arrange
        var envelopeId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "1",
                Name = "Doc 1",
                Order = 1,
                IsActive = true
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "2",
                Name = "Doc 2",
                Order = 2,
                IsActive = true
            }
        }.AsQueryable();

        var mockDocumentSet = CreateMockDbSet(documents);
        _mockContext.Setup(c => c.Documents).Returns(mockDocumentSet.Object);

        // Act
        var result = await _controller.GetEnvelopeDocuments(envelopeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDocuments = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value);
        Assert.Equal(2, returnedDocuments.Count());
    }

    [Fact]
    public async Task GetEnvelopeDocuments_ReturnsDocumentsInOrder()
    {
        // Arrange
        var envelopeId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "2",
                Name = "Doc 2",
                Order = 2,
                IsActive = true
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "1",
                Name = "Doc 1",
                Order = 1,
                IsActive = true
            }
        }.AsQueryable();

        var mockDocumentSet = CreateMockDbSet(documents);
        _mockContext.Setup(c => c.Documents).Returns(mockDocumentSet.Object);

        // Act
        var result = await _controller.GetEnvelopeDocuments(envelopeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDocuments = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value).ToList();
        Assert.Equal("1", returnedDocuments[0].DocuSignDocumentId);
        Assert.Equal("2", returnedDocuments[1].DocuSignDocumentId);
    }

    [Fact]
    public async Task GetEnvelopeDocuments_OnlyReturnsActiveDocuments()
    {
        // Arrange
        var envelopeId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "1",
                Name = "Doc 1",
                Order = 1,
                IsActive = true
            },
            new Document
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelopeId,
                DocuSignDocumentId = "2",
                Name = "Doc 2",
                Order = 2,
                IsActive = false
            }
        }.AsQueryable();

        var mockDocumentSet = CreateMockDbSet(documents);
        _mockContext.Setup(c => c.Documents).Returns(mockDocumentSet.Object);

        // Act
        var result = await _controller.GetEnvelopeDocuments(envelopeId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDocuments = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value);
        Assert.Single(returnedDocuments);
    }
}

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(resultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}
