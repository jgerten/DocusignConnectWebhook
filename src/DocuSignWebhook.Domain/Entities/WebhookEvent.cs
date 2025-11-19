namespace DocuSignWebhook.Domain.Entities;

/// <summary>
/// Represents a webhook event received from DocuSign Connect
/// </summary>
public class WebhookEvent : BaseEntity
{
    /// <summary>
    /// DocuSign event type (e.g., "envelope-completed", "envelope-sent", "recipient-completed")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// DocuSign envelope ID from the webhook
    /// </summary>
    public string EnvelopeId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the envelope (e.g., "completed", "sent", "voided")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Raw JSON payload from DocuSign Connect webhook
    /// </summary>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>
    /// Processing status of this webhook event
    /// </summary>
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of times processing has been attempted
    /// </summary>
    public int ProcessingAttempts { get; set; } = 0;

    /// <summary>
    /// When this event was last processed or attempted
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Related envelope (if processed successfully)
    /// </summary>
    public Guid? EnvelopeEntityId { get; set; }
    public Envelope? Envelope { get; set; }
}

public enum WebhookProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Ignored = 4  // For events we don't care about
}
