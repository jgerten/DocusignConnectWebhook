using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Tests.Domain;

public class WebhookEventTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var webhookEvent = new WebhookEvent();

        // Assert
        Assert.Equal(string.Empty, webhookEvent.EventType);
        Assert.Equal(string.Empty, webhookEvent.EnvelopeId);
        Assert.Equal(string.Empty, webhookEvent.Status);
        Assert.Equal(string.Empty, webhookEvent.RawPayload);
        Assert.Equal(WebhookProcessingStatus.Pending, webhookEvent.ProcessingStatus);
        Assert.Equal(0, webhookEvent.ProcessingAttempts);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var webhookEvent = new WebhookEvent();
        var testDate = DateTime.UtcNow;
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope { Id = envelopeId };

        // Act
        webhookEvent.EventType = "envelope-completed";
        webhookEvent.EnvelopeId = "env123";
        webhookEvent.Status = "completed";
        webhookEvent.RawPayload = "{\"event\":\"test\"}";
        webhookEvent.ProcessingStatus = WebhookProcessingStatus.Completed;
        webhookEvent.ErrorMessage = "Test error";
        webhookEvent.ProcessingAttempts = 3;
        webhookEvent.ProcessedAt = testDate;
        webhookEvent.EnvelopeEntityId = envelopeId;
        webhookEvent.Envelope = envelope;

        // Assert
        Assert.Equal("envelope-completed", webhookEvent.EventType);
        Assert.Equal("env123", webhookEvent.EnvelopeId);
        Assert.Equal("completed", webhookEvent.Status);
        Assert.Equal("{\"event\":\"test\"}", webhookEvent.RawPayload);
        Assert.Equal(WebhookProcessingStatus.Completed, webhookEvent.ProcessingStatus);
        Assert.Equal("Test error", webhookEvent.ErrorMessage);
        Assert.Equal(3, webhookEvent.ProcessingAttempts);
        Assert.Equal(testDate, webhookEvent.ProcessedAt);
        Assert.Equal(envelopeId, webhookEvent.EnvelopeEntityId);
        Assert.Equal(envelope, webhookEvent.Envelope);
    }

    [Fact]
    public void NullableProperties_CanBeNull()
    {
        // Act
        var webhookEvent = new WebhookEvent();

        // Assert
        Assert.Null(webhookEvent.ErrorMessage);
        Assert.Null(webhookEvent.ProcessedAt);
        Assert.Null(webhookEvent.EnvelopeEntityId);
        Assert.Null(webhookEvent.Envelope);
    }

    [Theory]
    [InlineData(WebhookProcessingStatus.Pending)]
    [InlineData(WebhookProcessingStatus.Processing)]
    [InlineData(WebhookProcessingStatus.Completed)]
    [InlineData(WebhookProcessingStatus.Failed)]
    [InlineData(WebhookProcessingStatus.Ignored)]
    public void ProcessingStatus_AllEnumValues_CanBeSet(WebhookProcessingStatus status)
    {
        // Arrange
        var webhookEvent = new WebhookEvent();

        // Act
        webhookEvent.ProcessingStatus = status;

        // Assert
        Assert.Equal(status, webhookEvent.ProcessingStatus);
    }
}
