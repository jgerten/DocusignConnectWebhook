using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Application.Interfaces.Services;

/// <summary>
/// Service for processing DocuSign webhook events
/// </summary>
public interface IWebhookProcessor
{
    /// <summary>
    /// Processes a webhook event asynchronously
    /// </summary>
    /// <param name="webhookEventId">ID of the webhook event to process</param>
    Task ProcessWebhookEventAsync(Guid webhookEventId);

    /// <summary>
    /// Validates HMAC signature from DocuSign Connect
    /// </summary>
    /// <param name="payload">Raw webhook payload</param>
    /// <param name="signature">HMAC signature from header</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateHmacSignature(string payload, string signature);

    /// <summary>
    /// Processes envelope completion - downloads documents and stores in MinIO
    /// </summary>
    /// <param name="envelope">Envelope entity to process</param>
    Task ProcessEnvelopeCompletionAsync(Envelope envelope);
}
