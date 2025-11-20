using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace DocuSignWebhook.API.Controllers;

/// <summary>
/// Controller for receiving DocuSign Connect webhooks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocuSignWebhookController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IWebhookProcessor _webhookProcessor;
    private readonly ILogger<DocuSignWebhookController> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DocuSignWebhookController(
        IApplicationDbContext context,
        IWebhookProcessor webhookProcessor,
        ILogger<DocuSignWebhookController> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _webhookProcessor = webhookProcessor;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

/// <summary>
/// Receives webhook events from DocuSign Connect
/// </summary>
/// <returns>200 OK if webhook is accepted</returns>
[HttpPost]
public async Task<IActionResult> ReceiveWebhook()
{
    try
    {
        string rawPayload;
        
        // Read the raw body
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            rawPayload = await reader.ReadToEndAsync();
        }

        _logger.LogInformation("Received DocuSign webhook: {Payload}", rawPayload);

        // Validate HMAC signature if present
        if (Request.Headers.TryGetValue("X-DocuSign-Signature-1", out var signature))
        {
            if (!_webhookProcessor.ValidateHmacSignature(rawPayload, signature!))
            {
                _logger.LogWarning("Invalid HMAC signature for webhook");
                return Unauthorized("Invalid signature");
            }
        }

        // Parse JSON payload (DocuSign Connect REST v2.1 format)
        string eventType = "unknown";
        string envelopeId = "unknown";
        string status = "unknown";

        try
        {
            var jsonDoc = JsonDocument.Parse(rawPayload);
            var root = jsonDoc.RootElement;

            // Get event type
            if (root.TryGetProperty("event", out var eventProp))
            {
                eventType = eventProp.GetString() ?? "unknown";
            }

            // Get envelope ID
            if (root.TryGetProperty("envelopeId", out var envIdProp))
            {
                envelopeId = envIdProp.GetString() ?? "unknown";
            }

            // Get status from envelopeSummary or root level
            if (root.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.TryGetProperty("envelopeSummary", out var summaryProp))
                {
                    if (summaryProp.TryGetProperty("status", out var statusProp))
                    {
                        status = statusProp.GetString() ?? "unknown";
                    }
                }
                if (dataProp.TryGetProperty("envelopeId", out var dataEnvId))
                {
                    envelopeId = dataEnvId.GetString() ?? envelopeId;
                }
            }

            if (root.TryGetProperty("status", out var rootStatusProp))
            {
                status = rootStatusProp.GetString() ?? status;
            }

            _logger.LogInformation("Event: {EventType}, Envelope: {EnvelopeId}, Status: {Status}", 
                eventType, envelopeId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing JSON payload, will store raw data");
        }

        // Create webhook event record
        var webhookEvent = new WebhookEvent
        {
            EventType = eventType,
            EnvelopeId = envelopeId,
            Status = status,
            RawPayload = rawPayload,
            ProcessingStatus = WebhookProcessingStatus.Pending
        };

        _context.WebhookEvents.Add(webhookEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved webhook event {WebhookEventId} for envelope {EnvelopeId}",
            webhookEvent.Id, envelopeId);

        // Process asynchronously with a new scope
        _ = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IWebhookProcessor>();

            try
            {
                await processor.ProcessWebhookEventAsync(webhookEvent.Id);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<DocuSignWebhookController>>();
                logger.LogError(ex, "Error processing webhook event {WebhookEventId}", webhookEvent.Id);
            }
        });

        return Ok();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling webhook");
        return StatusCode(500, "Error processing webhook");
    }
}

    /// <summary>
    /// Gets webhook event status
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetWebhookEvent(Guid id)
    {
        var webhookEvent = await _context.WebhookEvents.FindAsync(id);

        if (webhookEvent == null)
            return NotFound();

        return Ok(new
        {
            webhookEvent.Id,
            webhookEvent.EventType,
            webhookEvent.EnvelopeId,
            webhookEvent.Status,
            webhookEvent.ProcessingStatus,
            webhookEvent.ProcessingAttempts,
            webhookEvent.ErrorMessage,
            webhookEvent.CreatedAt,
            webhookEvent.ProcessedAt
        });
    }

    /// <summary>
    /// Health check endpoint for DocuSign Connect configuration
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "DocuSign Webhook API"
        });
    }

    /// <summary>
    /// Manually retry a failed webhook event
    /// </summary>
    /// <param name="id">Webhook event ID to retry</param>
    [HttpPost("{id}/retry")]
    public async Task<IActionResult> RetryWebhookEvent(Guid id)
    {
        var webhookEvent = await _context.WebhookEvents.FindAsync(id);

        if (webhookEvent == null)
            return NotFound(new { message = "Webhook event not found" });

        if (webhookEvent.ProcessingStatus == WebhookProcessingStatus.Completed)
            return BadRequest(new { message = "Webhook event already completed successfully" });

        if (webhookEvent.ProcessingStatus == WebhookProcessingStatus.Processing)
            return BadRequest(new { message = "Webhook event is currently being processed" });

        _logger.LogInformation("Manual retry triggered for webhook event {WebhookEventId}", id);

        // Reset status to pending
        webhookEvent.ProcessingStatus = WebhookProcessingStatus.Pending;
        webhookEvent.ErrorMessage = null;
        await _context.SaveChangesAsync();

        // Trigger processing asynchronously
        _ = Task.Run(async () =>
        {
            try
            {
                await _webhookProcessor.ProcessWebhookEventAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual retry of webhook event {WebhookEventId}", id);
            }
        });

        return Ok(new { message = "Retry triggered", webhookEventId = id });
    }
}
