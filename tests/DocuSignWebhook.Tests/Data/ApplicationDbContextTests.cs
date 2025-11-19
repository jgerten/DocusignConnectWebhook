using DocuSignWebhook.Domain.Entities;
using DocuSignWebhook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocuSignWebhook.Tests.Data;

public class ApplicationDbContextTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Constructor_ShouldInitializeDbSets()
    {
        // Arrange & Act
        using var context = CreateContext();

        // Assert
        Assert.NotNull(context.WebhookEvents);
        Assert.NotNull(context.Envelopes);
        Assert.NotNull(context.Documents);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldUpdateTimestamp_WhenEntityIsModified()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "sent",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };

        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var originalUpdatedAt = envelope.UpdatedAt;
        Assert.Null(originalUpdatedAt);

        // Act
        await Task.Delay(10); // Small delay to ensure timestamp changes
        envelope.Subject = "Modified Subject";
        context.Envelopes.Update(envelope);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotNull(envelope.UpdatedAt);
        Assert.True(envelope.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task WebhookEvent_CanBeAdded()
    {
        // Arrange
        using var context = CreateContext();
        var webhookEvent = new WebhookEvent
        {
            EventType = "test-event",
            EnvelopeId = "env123",
            Status = "pending",
            RawPayload = "{\"test\":\"data\"}"
        };

        // Act
        context.WebhookEvents.Add(webhookEvent);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.WebhookEvents.FindAsync(webhookEvent.Id);
        Assert.NotNull(saved);
        Assert.Equal("test-event", saved.EventType);
    }

    [Fact]
    public async Task Envelope_CanBeAdded()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test Envelope",
            Status = "sent",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };

        // Act
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Envelopes.FindAsync(envelope.Id);
        Assert.NotNull(saved);
        Assert.Equal("env123", saved.DocuSignEnvelopeId);
    }

    [Fact]
    public async Task Document_CanBeAdded()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "completed",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var document = new Document
        {
            DocuSignDocumentId = "doc123",
            Name = "test.pdf",
            DocumentType = "pdf",
            FileExtension = "pdf",
            EnvelopeId = envelope.Id
        };

        // Act
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.Documents.FindAsync(document.Id);
        Assert.NotNull(saved);
        Assert.Equal("doc123", saved.DocuSignDocumentId);
    }

    [Fact]
    public async Task Envelope_CascadeDelete_DeletesDocuments()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "completed",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var document = new Document
        {
            DocuSignDocumentId = "doc123",
            Name = "test.pdf",
            DocumentType = "pdf",
            FileExtension = "pdf",
            EnvelopeId = envelope.Id
        };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var documentId = document.Id;

        // Act
        context.Envelopes.Remove(envelope);
        await context.SaveChangesAsync();

        // Assert
        var deletedDocument = await context.Documents.FindAsync(documentId);
        Assert.Null(deletedDocument);
    }

    [Fact]
    public async Task WebhookEvent_SetNull_OnEnvelopeDelete()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "completed",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var webhookEvent = new WebhookEvent
        {
            EventType = "envelope-completed",
            EnvelopeId = "env123",
            Status = "completed",
            RawPayload = "{\"test\":\"data\"}",
            EnvelopeEntityId = envelope.Id
        };
        context.WebhookEvents.Add(webhookEvent);
        await context.SaveChangesAsync();

        var webhookEventId = webhookEvent.Id;

        // Act
        context.Envelopes.Remove(envelope);
        await context.SaveChangesAsync();

        // Assert
        var updatedWebhookEvent = await context.WebhookEvents.FindAsync(webhookEventId);
        Assert.NotNull(updatedWebhookEvent);
        Assert.Null(updatedWebhookEvent.EnvelopeEntityId);
    }

    [Fact]
    public async Task Envelope_UniqueIndex_OnDocuSignEnvelopeId()
    {
        // Arrange
        using var context = CreateContext();
        var envelope1 = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test 1",
            Status = "sent",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope1);
        await context.SaveChangesAsync();

        var envelope2 = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test 2",
            Status = "sent",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope2);

        // Act & Assert
        await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task ModelConfiguration_AppliesCorrectly()
    {
        // Arrange
        using var context = CreateContext();
        var model = context.Model;

        // Act
        var webhookEventType = model.FindEntityType(typeof(WebhookEvent));
        var envelopeType = model.FindEntityType(typeof(Envelope));
        var documentType = model.FindEntityType(typeof(Document));

        // Assert
        Assert.NotNull(webhookEventType);
        Assert.NotNull(envelopeType);
        Assert.NotNull(documentType);

        // Verify indexes exist
        Assert.NotEmpty(webhookEventType.GetIndexes());
        Assert.NotEmpty(envelopeType.GetIndexes());
        Assert.NotEmpty(documentType.GetIndexes());
    }

    [Fact]
    public async Task Envelope_WithDocuments_CanBeQueried()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "completed",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope);
        await context.SaveChangesAsync();

        var document1 = new Document
        {
            DocuSignDocumentId = "1",
            Name = "doc1.pdf",
            DocumentType = "pdf",
            FileExtension = "pdf",
            EnvelopeId = envelope.Id
        };
        var document2 = new Document
        {
            DocuSignDocumentId = "2",
            Name = "doc2.pdf",
            DocumentType = "pdf",
            FileExtension = "pdf",
            EnvelopeId = envelope.Id
        };
        context.Documents.AddRange(document1, document2);
        await context.SaveChangesAsync();

        // Act
        var queriedEnvelope = await context.Envelopes
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.DocuSignEnvelopeId == "env123");

        // Assert
        Assert.NotNull(queriedEnvelope);
        Assert.Equal(2, queriedEnvelope.Documents.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var context = CreateContext();
        var envelope = new Envelope
        {
            DocuSignEnvelopeId = "env123",
            Subject = "Test",
            Status = "sent",
            SenderEmail = "test@example.com",
            SenderName = "Test User"
        };
        context.Envelopes.Add(envelope);

        var cts = new CancellationTokenSource();

        // Act
        await context.SaveChangesAsync(cts.Token);

        // Assert
        var saved = await context.Envelopes.FindAsync(envelope.Id);
        Assert.NotNull(saved);
    }
}
