using DocuSignWebhook.API.Controllers;
using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace DocuSignWebhook.Tests.Controllers;

public class DocuSignWebhookControllerTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly Mock<IWebhookProcessor> _mockWebhookProcessor;
    private readonly Mock<ILogger<DocuSignWebhookController>> _mockLogger;
    private readonly DocuSignWebhookController _controller;

    public DocuSignWebhookControllerTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _mockWebhookProcessor = new Mock<IWebhookProcessor>();
        _mockLogger = new Mock<ILogger<DocuSignWebhookController>>();

        SetupMockDbSets();

        _controller = new DocuSignWebhookController(
            _mockContext.Object,
            _mockWebhookProcessor.Object,
            _mockLogger.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetupMockDbSets()
    {
        var webhookEvents = new List<WebhookEvent>().AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mockSet;
    }

    [Fact]
    public async Task ReceiveWebhook_WithValidPayload_ReturnsOk()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"event\":\"envelope-completed\",\"data\":{\"envelopeId\":\"env123\"}}");
        _mockWebhookProcessor.Setup(x => x.ValidateHmacSignature(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveWebhook_WithInvalidSignature_ReturnsUnauthorized()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"event\":\"test\"}");
        _controller.ControllerContext.HttpContext.Request.Headers["X-DocuSign-Signature-1"] = "invalid-signature";
        _mockWebhookProcessor.Setup(x => x.ValidateHmacSignature(It.IsAny<string>(), "invalid-signature"))
            .Returns(false);

        // Act
        var result = await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid signature", unauthorizedResult.Value);
    }

    [Fact]
    public async Task ReceiveWebhook_WithValidSignature_ReturnsOk()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"event\":\"envelope-completed\"}");
        _controller.ControllerContext.HttpContext.Request.Headers["X-DocuSign-Signature-1"] = "valid-signature";
        _mockWebhookProcessor.Setup(x => x.ValidateHmacSignature(It.IsAny<string>(), "valid-signature"))
            .Returns(true);

        // Act
        var result = await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReceiveWebhook_ParsesEventType()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"event\":\"envelope-sent\",\"data\":{\"envelopeId\":\"env456\"}}");
        WebhookEvent? capturedEvent = null;
        _mockContext.Setup(x => x.WebhookEvents.Add(It.IsAny<WebhookEvent>()))
            .Callback<WebhookEvent>(e => capturedEvent = e);

        // Act
        await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("envelope-sent", capturedEvent.EventType);
        Assert.Equal("env456", capturedEvent.EnvelopeId);
    }

    [Fact]
    public async Task ReceiveWebhook_ParsesAlternativePayloadStructure()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"envelopeId\":\"env789\",\"status\":\"voided\"}");
        WebhookEvent? capturedEvent = null;
        _mockContext.Setup(x => x.WebhookEvents.Add(It.IsAny<WebhookEvent>()))
            .Callback<WebhookEvent>(e => capturedEvent = e);

        // Act
        await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("env789", capturedEvent.EnvelopeId);
        Assert.Equal("voided", capturedEvent.Status);
    }

    [Fact]
    public async Task ReceiveWebhook_WithParsingError_StillSavesEvent()
    {
        // Arrange
        var payload = JsonDocument.Parse("{\"invalid\":\"structure\"}");
        WebhookEvent? capturedEvent = null;
        _mockContext.Setup(x => x.WebhookEvents.Add(It.IsAny<WebhookEvent>()))
            .Callback<WebhookEvent>(e => capturedEvent = e);

        // Act
        await _controller.ReceiveWebhook(payload.RootElement);

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("unknown", capturedEvent.EventType);
        Assert.Equal("unknown", capturedEvent.EnvelopeId);
    }

    [Fact]
    public async Task GetWebhookEvent_WithExistingEvent_ReturnsOk()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var webhookEvent = new WebhookEvent
        {
            Id = eventId,
            EventType = "test-event",
            EnvelopeId = "env123",
            Status = "completed"
        };

        var webhookEvents = new List<WebhookEvent> { webhookEvent }.AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        mockWebhookSet.Setup(x => x.FindAsync(eventId)).ReturnsAsync(webhookEvent);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        // Act
        var result = await _controller.GetWebhookEvent(eventId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetWebhookEvent_WithNonExistentEvent_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var webhookEvents = new List<WebhookEvent>().AsQueryable();
        var mockWebhookSet = CreateMockDbSet(webhookEvents);
        mockWebhookSet.Setup(x => x.FindAsync(eventId)).ReturnsAsync((WebhookEvent?)null);
        _mockContext.Setup(c => c.WebhookEvents).Returns(mockWebhookSet.Object);

        // Act
        var result = await _controller.GetWebhookEvent(eventId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void HealthCheck_ReturnsOk()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void HealthCheck_ReturnsCorrectStructure()
    {
        // Act
        var result = _controller.HealthCheck();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        var statusProperty = value.GetType().GetProperty("status");
        var timestampProperty = value.GetType().GetProperty("timestamp");
        var serviceProperty = value.GetType().GetProperty("service");

        Assert.NotNull(statusProperty);
        Assert.NotNull(timestampProperty);
        Assert.NotNull(serviceProperty);

        Assert.Equal("healthy", statusProperty.GetValue(value));
        Assert.Equal("DocuSign Webhook API", serviceProperty.GetValue(value));
    }
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(_inner.MoveNext());
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}
