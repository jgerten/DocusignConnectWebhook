using DocuSignWebhook.Application.Interfaces;
using DocuSignWebhook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocuSignWebhook.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<Envelope> Envelopes => Set<Envelope>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WebhookEvent configuration
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EnvelopeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RawPayload).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.HasIndex(e => e.EnvelopeId);
            entity.HasIndex(e => e.ProcessingStatus);
            entity.HasIndex(e => new { e.EnvelopeId, e.EventType });

            entity.HasOne(e => e.Envelope)
                .WithMany(env => env.WebhookEvents)
                .HasForeignKey(e => e.EnvelopeEntityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Envelope configuration
        modelBuilder.Entity<Envelope>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocuSignEnvelopeId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SenderEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.SenderName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.VoidedReason).HasMaxLength(500);

            entity.HasIndex(e => e.DocuSignEnvelopeId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CompletedAt);

            entity.HasMany(e => e.Documents)
                .WithOne(d => d.Envelope)
                .HasForeignKey(d => d.EnvelopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocuSignDocumentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MinioBucket).HasMaxLength(100);
            entity.Property(e => e.MinioObjectKey).HasMaxLength(500);
            entity.Property(e => e.ContentHash).HasMaxLength(100);

            entity.HasIndex(e => new { e.EnvelopeId, e.DocuSignDocumentId });
            entity.HasIndex(e => e.MinioObjectKey);
            entity.HasIndex(e => e.UploadedToMinIO);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
