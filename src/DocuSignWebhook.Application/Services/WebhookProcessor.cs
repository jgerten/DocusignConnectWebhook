using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DocuSignWebhook.Application.Services;

/// <summary>
/// Implementation of webhook processing logic
/// </summary>
public class WebhookProcessor : IWebhookProcessor
{
    private readonly IApplicationDbContext _context;
    private readonly IDocuSignService _docuSignService;
    private readonly IMinioStorageService _minioService;
    private readonly ILogger<WebhookProcessor> _logger;
    private readonly string _hmacSecret;
    private readonly string _defaultBucket;

    public WebhookProcessor(
        IApplicationDbContext context,
        IDocuSignService docuSignService,
        IMinioStorageService minioService,
        ILogger<WebhookProcessor> logger,
        string hmacSecret,
        string defaultBucket = "docusign-documents")
    {
        _context = context;
        _docuSignService = docuSignService;
        _minioService = minioService;
        _logger = logger;
        _hmacSecret = hmacSecret;
        _defaultBucket = defaultBucket;
    }

    public bool ValidateHmacSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_hmacSecret))
        {
            _logger.LogWarning("HMAC secret not configured - skipping validation");
            return true; // In dev mode, might not have HMAC configured
        }

        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToBase64String(hash);

            return computedSignature == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating HMAC signature");
            return false;
        }
    }

    public async Task ProcessWebhookEventAsync(Guid webhookEventId)
    {
        var webhookEvent = await _context.WebhookEvents
            .FirstOrDefaultAsync(w => w.Id == webhookEventId);

        if (webhookEvent == null)
        {
            _logger.LogWarning("Webhook event {WebhookEventId} not found", webhookEventId);
            return;
        }

        // Update status to processing
        webhookEvent.ProcessingStatus = WebhookProcessingStatus.Processing;
        webhookEvent.ProcessingAttempts++;
        await _context.SaveChangesAsync();

        try
        {
            // Parse the webhook payload
            var payload = JsonDocument.Parse(webhookEvent.RawPayload);

            // Check if this is an event we care about
            if (webhookEvent.EventType == "envelope-completed" || webhookEvent.Status == "completed")
            {
                // Get or create envelope
                var envelope = await GetOrCreateEnvelopeAsync(webhookEvent);

                if (envelope != null && !envelope.DocumentsDownloaded)
                {
                    await ProcessEnvelopeCompletionAsync(envelope);
                    webhookEvent.EnvelopeEntityId = envelope.Id;
                }
                else if (envelope != null && envelope.DocumentsDownloaded)
                {
                    _logger.LogInformation("Envelope {EnvelopeId} documents already downloaded - skipping",
                        envelope.DocuSignEnvelopeId);
                }
            }
            else
            {
                _logger.LogInformation("Webhook event {EventType} for envelope {EnvelopeId} - no action needed",
                    webhookEvent.EventType, webhookEvent.EnvelopeId);
                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Ignored;
            }

            // Only mark as completed if not already marked as ignored
            if (webhookEvent.ProcessingStatus != WebhookProcessingStatus.Ignored)
            {
                webhookEvent.ProcessingStatus = WebhookProcessingStatus.Completed;
            }

            webhookEvent.ProcessedAt = DateTime.UtcNow;
            webhookEvent.ErrorMessage = null; // Clear any previous error messages
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed webhook event {WebhookEventId} (attempt {Attempt})",
                webhookEventId, webhookEvent.ProcessingAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {WebhookEventId} (attempt {Attempt})",
                webhookEventId, webhookEvent.ProcessingAttempts);

            webhookEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
            webhookEvent.ErrorMessage = $"[Attempt {webhookEvent.ProcessingAttempts}] {ex.Message}";
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Re-throw to allow caller to handle if needed
            throw;
        }
    }

    public async Task ProcessEnvelopeCompletionAsync(Envelope envelope)
    {
        _logger.LogInformation("Processing completion for envelope {EnvelopeId}", envelope.DocuSignEnvelopeId);

        // Ensure MinIO bucket exists
        await _minioService.EnsureBucketExistsAsync(_defaultBucket);

        // Get documents list from DocuSign
        var documents = await _docuSignService.ListEnvelopeDocumentsAsync(envelope.DocuSignEnvelopeId);

        foreach (var document in documents)
        {
            try
            {
                // Download document from DocuSign
                _logger.LogInformation("Downloading document {DocumentId} from envelope {EnvelopeId}",
                    document.DocuSignDocumentId, envelope.DocuSignEnvelopeId);

                var fileData = await _docuSignService.DownloadDocumentAsync(
                    envelope.DocuSignEnvelopeId,
                    document.DocuSignDocumentId);

                document.FileSizeBytes = fileData.Length;

                // Compute hash
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(fileData);
                document.ContentHash = Convert.ToBase64String(hash);

                // Generate MinIO object key
                var objectKey = $"{envelope.DocuSignEnvelopeId}/{document.DocuSignDocumentId}_{document.Name}";
                document.MinioObjectKey = objectKey;
                document.MinioBucket = _defaultBucket;

                // Upload to MinIO
                _logger.LogInformation("Uploading document to MinIO: {ObjectKey}", objectKey);
                await _minioService.UploadFileAsync(
                    _defaultBucket,
                    objectKey,
                    fileData,
                    "application/pdf");

                document.UploadedToMinIO = true;
                document.UploadedAt = DateTime.UtcNow;
                document.EnvelopeId = envelope.Id;

                // Add to database
                _context.Documents.Add(document);

                _logger.LogInformation("Successfully processed document {DocumentId}", document.DocuSignDocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId} from envelope {EnvelopeId}",
                    document.DocuSignDocumentId, envelope.DocuSignEnvelopeId);
                throw;
            }
        }

        envelope.DocumentsDownloaded = true;
        envelope.DocumentsDownloadedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Completed processing envelope {EnvelopeId} - {DocumentCount} documents",
            envelope.DocuSignEnvelopeId, documents.Count);
    }

    private async Task<Envelope?> GetOrCreateEnvelopeAsync(WebhookEvent webhookEvent)
    {
        // Check if envelope already exists
        var envelope = await _context.Envelopes
            .FirstOrDefaultAsync(e => e.DocuSignEnvelopeId == webhookEvent.EnvelopeId);

        if (envelope != null)
        {
            return envelope;
        }

        // Create new envelope from DocuSign API
        try
        {
            envelope = await _docuSignService.GetEnvelopeDetailsAsync(webhookEvent.EnvelopeId);
            _context.Envelopes.Add(envelope);
            await _context.SaveChangesAsync();
            return envelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting envelope details for {EnvelopeId}", webhookEvent.EnvelopeId);
            return null;
        }
    }
}
