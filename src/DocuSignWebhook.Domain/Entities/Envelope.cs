namespace DocuSignWebhook.Domain.Entities;

/// <summary>
/// Represents a DocuSign envelope with its metadata
/// </summary>
public class Envelope : BaseEntity
{
    /// <summary>
    /// DocuSign envelope ID (from DocuSign API)
    /// </summary>
    public string DocuSignEnvelopeId { get; set; } = string.Empty;

    /// <summary>
    /// Envelope subject/title
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Current status (e.g., "completed", "sent", "voided")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the sender
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Name of the sender
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// When the envelope was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the envelope was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// When the envelope was voided (if applicable)
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// Reason for voiding (if applicable)
    /// </summary>
    public string? VoidedReason { get; set; }

    /// <summary>
    /// Whether documents have been downloaded from DocuSign
    /// </summary>
    public bool DocumentsDownloaded { get; set; } = false;

    /// <summary>
    /// When documents were downloaded
    /// </summary>
    public DateTime? DocumentsDownloadedAt { get; set; }

    /// <summary>
    /// Additional metadata as JSON
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Documents contained in this envelope
    /// </summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();

    /// <summary>
    /// Webhook events related to this envelope
    /// </summary>
    public ICollection<WebhookEvent> WebhookEvents { get; set; } = new List<WebhookEvent>();
}
