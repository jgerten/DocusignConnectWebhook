using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Tests.Domain;

public class DocumentTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        Assert.Equal(string.Empty, document.DocuSignDocumentId);
        Assert.Equal(string.Empty, document.Name);
        Assert.Equal(string.Empty, document.DocumentType);
        Assert.Equal(string.Empty, document.FileExtension);
        Assert.Equal(0, document.Order);
        Assert.False(document.UploadedToMinIO);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var document = new Document();
        var testDate = DateTime.UtcNow;
        var envelopeId = Guid.NewGuid();
        var envelope = new Envelope { Id = envelopeId };

        // Act
        document.DocuSignDocumentId = "doc123";
        document.Name = "Test Document.pdf";
        document.DocumentType = "pdf";
        document.FileExtension = "pdf";
        document.Order = 1;
        document.FileSizeBytes = 1024;
        document.PageCount = 5;
        document.MinioBucket = "test-bucket";
        document.MinioObjectKey = "test/key";
        document.ContentHash = "abc123";
        document.UploadedToMinIO = true;
        document.UploadedAt = testDate;
        document.EnvelopeId = envelopeId;
        document.Envelope = envelope;

        // Assert
        Assert.Equal("doc123", document.DocuSignDocumentId);
        Assert.Equal("Test Document.pdf", document.Name);
        Assert.Equal("pdf", document.DocumentType);
        Assert.Equal("pdf", document.FileExtension);
        Assert.Equal(1, document.Order);
        Assert.Equal(1024, document.FileSizeBytes);
        Assert.Equal(5, document.PageCount);
        Assert.Equal("test-bucket", document.MinioBucket);
        Assert.Equal("test/key", document.MinioObjectKey);
        Assert.Equal("abc123", document.ContentHash);
        Assert.True(document.UploadedToMinIO);
        Assert.Equal(testDate, document.UploadedAt);
        Assert.Equal(envelopeId, document.EnvelopeId);
        Assert.Equal(envelope, document.Envelope);
    }

    [Fact]
    public void NullableProperties_CanBeNull()
    {
        // Act
        var document = new Document();

        // Assert
        Assert.Null(document.FileSizeBytes);
        Assert.Null(document.PageCount);
        Assert.Null(document.MinioBucket);
        Assert.Null(document.MinioObjectKey);
        Assert.Null(document.ContentHash);
        Assert.Null(document.UploadedAt);
    }
}
