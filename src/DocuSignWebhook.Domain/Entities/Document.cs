namespace DocuSignWebhook.Domain.Entities;

/// <summary>
/// Represents a document within a DocuSign envelope
/// </summary>
public class Document : BaseEntity
{
    /// <summary>
    /// DocuSign document ID
    /// </summary>
    public string DocuSignDocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Document name/filename
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Document type (e.g., "pdf", "docx")
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Order/sequence of document in envelope
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Number of pages (if applicable)
    /// </summary>
    public int? PageCount { get; set; }

    /// <summary>
    /// MinIO bucket name where file is stored
    /// </summary>
    public string? MinioBucket { get; set; }

    /// <summary>
    /// MinIO object key/path
    /// </summary>
    public string? MinioObjectKey { get; set; }

    /// <summary>
    /// MD5 hash of the file content
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// Whether the file has been successfully uploaded to MinIO
    /// </summary>
    public bool UploadedToMinIO { get; set; } = false;

    /// <summary>
    /// When the file was uploaded to MinIO
    /// </summary>
    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Parent envelope
    /// </summary>
    public Guid EnvelopeId { get; set; }
    public Envelope Envelope { get; set; } = null!;
}
