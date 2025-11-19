using DocuSignWebhook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuSignWebhook.Application.Interfaces;

/// <summary>
/// Database context interface for dependency inversion
/// </summary>
public interface IApplicationDbContext
{
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<Envelope> Envelopes { get; }
    DbSet<Document> Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
