using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocuSignWebhook.Application.Services;

/// <summary>
/// Background service that automatically retries failed webhook events with exponential backoff
/// </summary>
public class WebhookRetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookRetryBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly int _maxRetryAttempts;
    private readonly int _baseDelayMinutes;
    private readonly int _batchSize;

    public WebhookRetryBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<WebhookRetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Read configuration with defaults
        _maxRetryAttempts = configuration.GetValue<int>("WebhookRetry:MaxRetryAttempts", 5);
        _baseDelayMinutes = configuration.GetValue<int>("WebhookRetry:BaseDelayMinutes", 2);
        var pollingIntervalSeconds = configuration.GetValue<int>("WebhookRetry:PollingIntervalSeconds", 60);
        _pollingInterval = TimeSpan.FromSeconds(pollingIntervalSeconds);
        _batchSize = configuration.GetValue<int>("WebhookRetry:BatchSize", 10);

        _logger.LogInformation(
            "Webhook Retry Service configured: MaxAttempts={MaxAttempts}, BaseDelay={BaseDelay}min, PollingInterval={PollingInterval}s, BatchSize={BatchSize}",
            _maxRetryAttempts, _baseDelayMinutes, pollingIntervalSeconds, _batchSize);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Retry Background Service started");

        // Wait a bit on startup to let the application fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFailedWebhooksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in webhook retry background service");
            }

            // Wait before next polling cycle
            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Webhook Retry Background Service stopped");
    }

    private async Task ProcessFailedWebhooksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Find failed webhook events that are eligible for retry
        var eligibleForRetry = await context.WebhookEvents
            .Where(w => w.ProcessingStatus == WebhookProcessingStatus.Failed)
            .Where(w => w.ProcessingAttempts < _maxRetryAttempts)
            .Where(w => w.ProcessedAt != null) // Must have been attempted at least once
            .OrderBy(w => w.ProcessedAt) // Process oldest first
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!eligibleForRetry.Any())
        {
            return; // No failed events to retry
        }

        _logger.LogInformation("Found {Count} failed webhook events eligible for retry", eligibleForRetry.Count);

        foreach (var webhookEvent in eligibleForRetry)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (ShouldRetryNow(webhookEvent))
            {
                await RetryWebhookEventAsync(webhookEvent, scope, cancellationToken);
            }
        }
    }

    private bool ShouldRetryNow(WebhookEvent webhookEvent)
    {
        if (webhookEvent.ProcessedAt == null)
            return false;

        // Calculate exponential backoff delay based on attempt number
        // Example with BaseDelayMinutes = 2:
        // Attempt 1: 2 minutes
        // Attempt 2: 4 minutes
        // Attempt 3: 8 minutes
        // Attempt 4: 16 minutes
        // Attempt 5: 32 minutes
        var delayMinutes = _baseDelayMinutes * Math.Pow(2, webhookEvent.ProcessingAttempts - 1);
        var requiredDelay = TimeSpan.FromMinutes(delayMinutes);

        var timeSinceLastAttempt = DateTime.UtcNow - webhookEvent.ProcessedAt.Value;

        if (timeSinceLastAttempt >= requiredDelay)
        {
            _logger.LogInformation(
                "Webhook event {WebhookEventId} ready for retry (attempt {Attempt}/{Max}, waited {Minutes:F1} minutes)",
                webhookEvent.Id,
                webhookEvent.ProcessingAttempts + 1,
                _maxRetryAttempts,
                timeSinceLastAttempt.TotalMinutes);
            return true;
        }

        return false;
    }

    private async Task RetryWebhookEventAsync(
        WebhookEvent webhookEvent,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var processor = scope.ServiceProvider.GetRequiredService<IWebhookProcessor>();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        try
        {
            _logger.LogInformation(
                "Retrying webhook event {WebhookEventId} for envelope {EnvelopeId} (attempt {Attempt}/{Max})",
                webhookEvent.Id,
                webhookEvent.EnvelopeId,
                webhookEvent.ProcessingAttempts + 1,
                _maxRetryAttempts);

            // Reset status to pending so the processor will handle it
            webhookEvent.ProcessingStatus = WebhookProcessingStatus.Pending;
            webhookEvent.ErrorMessage = null;
            await context.SaveChangesAsync(cancellationToken);

            // Process the webhook event
            await processor.ProcessWebhookEventAsync(webhookEvent.Id);

            _logger.LogInformation(
                "Successfully retried webhook event {WebhookEventId}",
                webhookEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to retry webhook event {WebhookEventId} (attempt {Attempt}/{Max})",
                webhookEvent.Id,
                webhookEvent.ProcessingAttempts,
                _maxRetryAttempts);

            // The processor will have already marked it as failed and incremented attempts
            // Check if we've exceeded max attempts
            var updatedEvent = await context.WebhookEvents.FindAsync(webhookEvent.Id);
            if (updatedEvent != null && updatedEvent.ProcessingAttempts >= _maxRetryAttempts)
            {
                _logger.LogWarning(
                    "Webhook event {WebhookEventId} has exceeded maximum retry attempts ({Max}). Manual intervention required.",
                    webhookEvent.Id,
                    _maxRetryAttempts);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Webhook Retry Background Service is stopping - waiting for current operations to complete");
        await base.StopAsync(cancellationToken);
    }
}
