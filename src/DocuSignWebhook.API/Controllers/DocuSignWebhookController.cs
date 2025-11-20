using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

    public DocuSignWebhookController(
        IApplicationDbContext context,
        IWebhookProcessor webhookProcessor,
        ILogger<DocuSignWebhookController> logger)
    {
        _context = context;
        _webhookProcessor = webhookProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Receives webhook events from DocuSign Connect
    /// </summary>
    /// <param name="payload">Webhook payload from DocuSign</param>
    /// <returns>200 OK if webhook is accepted</returns>
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload)
    {
        try
        {
            var rawPayload = payload.GetRawText();

            _logger.LogInformation("Received DocuSign webhook");

            // Validate HMAC signature if present
            if (Request.Headers.TryGetValue("X-DocuSign-Signature-1", out var signature))
            {
                if (!_webhookProcessor.ValidateHmacSignature(rawPayload, signature!))
                {
                    _logger.LogWarning("Invalid HMAC signature for webhook");
                    return Unauthorized("Invalid signature");
                }
            }

            // Extract basic info from payload
            string eventType = "unknown";
            string envelopeId = "unknown";
            string status = "unknown";

            try
            {
                if (payload.TryGetProperty("event", out var eventProp))
                {
                    eventType = eventProp.GetString() ?? "unknown";
                }

                if (payload.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("envelopeId", out var envId))
                    {
                        envelopeId = envId.GetString() ?? "unknown";
                    }
                    if (data.TryGetProperty("envelopeSummary", out var summary))
                    {
                        if (summary.TryGetProperty("status", out var statusProp))
                        {
                            status = statusProp.GetString() ?? "unknown";
                        }
                    }
                }

                // Alternative payload structure
                if (payload.TryGetProperty("envelopeId", out var altEnvId))
                {
                    envelopeId = altEnvId.GetString() ?? envelopeId;
                }
                if (payload.TryGetProperty("status", out var altStatus))
                {
                    status = altStatus.GetString() ?? status;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing webhook payload structure");
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

            // Process asynchronously (fire and forget - could use background service in production)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _webhookProcessor.ProcessWebhookEventAsync(webhookEvent.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing webhook event {WebhookEventId}", webhookEvent.Id);
                }
            });

            return Ok(new { message = "Webhook received", webhookEventId = webhookEvent.Id });
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
