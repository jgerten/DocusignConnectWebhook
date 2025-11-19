using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Application.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocuSignWebhook.Tests.Services;

public class WebhookProcessorTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<IDocuSignService> _mockDocuSignService;
    private readonly Mock<IMinioStorageService> _mockMinioService;
    private readonly Mock<ILogger<WebhookProcessor>> _mockLogger;
    private readonly WebhookProcessor _processor;
    private readonly string _hmacSecret = "test-secret";

    public WebhookProcessorTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockDocuSignService = new Mock<IDocuSignService>();
        _mockMinioService = new Mock<IMinioStorageService>();
        _mockLogger = new Mock<ILogger<WebhookProcessor>>();

        SetupMockDbSets();

        _processor = new WebhookProcessor(
            _mockContext.Object,
            _mockDocuSignService.Object,
            _mockMinioService.Object,
            _mockLogger.Object,
            _hmacSecret,
            "test-bucket");
    }

    private void SetupMockDbSets()
    {
        var webhookEvents = new List<WebhookEvent>().AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        var envelopes = new List<Envelope>().AsQueryable();
        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        var documents = new List<Document>().AsQueryable();
        var mockDocumentSet = CreateMockDbSet(documents);
        _mockContext.Setup(c => c.Documents).Returns(mockDocumentSet.Object);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }

    [Fact]
    public void ValidateHmacSignature_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_hmacSecret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        var signature = Convert.ToBase64String(hash);

        // Act
        var result = _processor.ValidateHmacSignature(payload, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateHmacSignature_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var payload = "{\"test\":\"data\"}";
        var signature = "invalid-signature";

        // Act
        var result = _processor.ValidateHmacSignature(payload, signature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateHmacSignature_WithEmptySecret_ReturnsTrue()
    {
        // Arrange
        var processor = new WebhookProcessor(
            _mockContext.Object,
            _mockDocuSignService.Object,
            _mockMinioService.Object,
            _mockLogger.Object,
            "",
            "test-bucket");
        var payload = "{\"test\":\"data\"}";
        var signature = "any-signature";

        // Act
        var result = processor.ValidateHmacSignature(payload, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_WithNonExistentEvent_LogsWarning()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var webhookEvents = new List<WebhookEvent>().AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        // Act
        await _processor.ProcessWebhookEventAsync(eventId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_WithCompletedEnvelope_ProcessesSuccessfully()
    {
        // Arrange
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            EventType = "envelope-completed",
            EnvelopeId = "env123",
            Status = "completed",
            RawPayload = "{\"event\":\"envelope-completed\",\"data\":{\"envelopeId\":\"env123\"}}"
        };

        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            DocumentsDownloaded = false
        };

        var document = new Document
        {
            DocuSignDocumentId = "1",
            Name = "test.pdf"
        };

        var webhookEvents = new List<WebhookEvent> { webhookEvent }.AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        var envelopes = new List<Envelope>().AsQueryable();
        var mockEnvelopeSet = CreateMockDbSet(envelopes);
        _mockContext.Setup(c => c.Envelopes).Returns(mockEnvelopeSet.Object);

        _mockDocuSignService.Setup(x => x.GetEnvelopeDetailsAsync("env123"))
            .ReturnsAsync(envelope);
        _mockDocuSignService.Setup(x => x.ListEnvelopeDocumentsAsync("env123"))
            .ReturnsAsync(new List<Document> { document });
        _mockDocuSignService.Setup(x => x.DownloadDocumentAsync("env123", "1"))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });

        // Act
        await _processor.ProcessWebhookEventAsync(webhookEvent.Id);

        // Assert
        _mockMinioService.Verify(x => x.EnsureBucketExistsAsync("test-bucket"), Times.Once);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_WithIgnoredEventType_SetsStatusToIgnored()
    {
        // Arrange
        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            EventType = "recipient-sent",
            EnvelopeId = "env123",
            Status = "sent",
            RawPayload = "{\"event\":\"recipient-sent\"}"
        };

        var webhookEvents = new List<WebhookEvent> { webhookEvent }.AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        // Act
        await _processor.ProcessWebhookEventAsync(webhookEvent.Id);

        // Assert
        Assert.Equal(WebhookProcessingStatus.Ignored, webhookEvent.ProcessingStatus);
    }

    [Fact]
    public async Task ProcessEnvelopeCompletionAsync_DownloadsAndUploadsDocuments()
    {
        // Arrange
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            DocumentsDownloaded = false
        };

        var document = new Document
        {
            DocuSignDocumentId = "1",
            Name = "test.pdf"
        };

        var fileData = new byte[] { 1, 2, 3, 4, 5 };

        _mockDocuSignService.Setup(x => x.ListEnvelopeDocumentsAsync("env123"))
            .ReturnsAsync(new List<Document> { document });
        _mockDocuSignService.Setup(x => x.DownloadDocumentAsync("env123", "1"))
            .ReturnsAsync(fileData);
        _mockMinioService.Setup(x => x.EnsureBucketExistsAsync("test-bucket"))
            .Returns(Task.CompletedTask);
        _mockMinioService.Setup(x => x.UploadFileAsync(
                "test-bucket",
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                "application/pdf"))
            .ReturnsAsync("test-bucket/env123/1_test.pdf");

        // Act
        await _processor.ProcessEnvelopeCompletionAsync(envelope);

        // Assert
        Assert.True(envelope.DocumentsDownloaded);
        Assert.NotNull(envelope.DocumentsDownloadedAt);
        _mockMinioService.Verify(x => x.UploadFileAsync(
            "test-bucket",
            It.IsAny<string>(),
            fileData,
            "application/pdf"), Times.Once);
    }

    [Fact]
    public async Task ProcessEnvelopeCompletionAsync_SetsDocumentProperties()
    {
        // Arrange
        var envelope = new Envelope
        {
            Id = Guid.NewGuid(),
            DocuSignEnvelopeId = "env123",
            DocumentsDownloaded = false
        };

        var document = new Document
        {
            DocuSignDocumentId = "1",
            Name = "test.pdf"
        };

        var fileData = new byte[] { 1, 2, 3, 4, 5 };

        _mockDocuSignService.Setup(x => x.ListEnvelopeDocumentsAsync("env123"))
            .ReturnsAsync(new List<Document> { document });
        _mockDocuSignService.Setup(x => x.DownloadDocumentAsync("env123", "1"))
            .ReturnsAsync(fileData);
        _mockMinioService.Setup(x => x.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>()))
            .ReturnsAsync("test-bucket/env123/1_test.pdf");

        // Act
        await _processor.ProcessEnvelopeCompletionAsync(envelope);

        // Assert
        Assert.Equal(fileData.Length, document.FileSizeBytes);
        Assert.NotNull(document.ContentHash);
        Assert.Equal("env123/1_test.pdf", document.MinioObjectKey);
        Assert.Equal("test-bucket", document.MinioBucket);
        Assert.True(document.UploadedToMinIO);
        Assert.NotNull(document.UploadedAt);
        Assert.Equal(envelope.Id, document.EnvelopeId);
    }
}
