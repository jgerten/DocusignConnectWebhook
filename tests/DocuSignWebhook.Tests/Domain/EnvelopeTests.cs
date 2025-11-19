using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Tests.Domain;

public class EnvelopeTests
{
    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        // Act
        var envelope = new Envelope();

        // Assert
        Assert.NotNull(envelope.Documents);
        Assert.Empty(envelope.Documents);
        Assert.NotNull(envelope.WebhookEvents);
        Assert.Empty(envelope.WebhookEvents);
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var envelope = new Envelope();

        // Assert
        Assert.Equal(string.Empty, envelope.DocuSignEnvelopeId);
        Assert.Equal(string.Empty, envelope.Subject);
        Assert.Equal(string.Empty, envelope.Status);
        Assert.Equal(string.Empty, envelope.SenderEmail);
        Assert.Equal(string.Empty, envelope.SenderName);
        Assert.False(envelope.DocumentsDownloaded);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var envelope = new Envelope();
        var testDate = DateTime.UtcNow;

        // Act
        envelope.DocuSignEnvelopeId = "env123";
        envelope.Subject = "Test Subject";
        envelope.Status = "completed";
        envelope.SenderEmail = "test@example.com";
        envelope.SenderName = "Test Sender";
        envelope.SentAt = testDate;
        envelope.CompletedAt = testDate;
        envelope.VoidedAt = testDate;
        envelope.VoidedReason = "Test reason";
        envelope.DocumentsDownloaded = true;
        envelope.DocumentsDownloadedAt = testDate;
        envelope.MetadataJson = "{\"key\":\"value\"}";

        // Assert
        Assert.Equal("env123", envelope.DocuSignEnvelopeId);
        Assert.Equal("Test Subject", envelope.Subject);
        Assert.Equal("completed", envelope.Status);
        Assert.Equal("test@example.com", envelope.SenderEmail);
        Assert.Equal("Test Sender", envelope.SenderName);
        Assert.Equal(testDate, envelope.SentAt);
        Assert.Equal(testDate, envelope.CompletedAt);
        Assert.Equal(testDate, envelope.VoidedAt);
        Assert.Equal("Test reason", envelope.VoidedReason);
        Assert.True(envelope.DocumentsDownloaded);
        Assert.Equal(testDate, envelope.DocumentsDownloadedAt);
        Assert.Equal("{\"key\":\"value\"}", envelope.MetadataJson);
    }

    [Fact]
    public void Documents_CanAddItems()
    {
        // Arrange
        var envelope = new Envelope();
        var document = new Document { DocuSignDocumentId = "doc1" };

        // Act
        envelope.Documents.Add(document);

        // Assert
        Assert.Single(envelope.Documents);
        Assert.Contains(document, envelope.Documents);
    }

    [Fact]
    public void WebhookEvents_CanAddItems()
    {
        // Arrange
        var envelope = new Envelope();
        var webhookEvent = new WebhookEvent { EventType = "test-event" };

        // Act
        envelope.WebhookEvents.Add(webhookEvent);

        // Assert
        Assert.Single(envelope.WebhookEvents);
        Assert.Contains(webhookEvent, envelope.WebhookEvents);
    }
}
