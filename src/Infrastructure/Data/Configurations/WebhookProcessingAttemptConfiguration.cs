using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class WebhookProcessingAttemptConfiguration
    : IEntityTypeConfiguration<WebhookProcessingAttempt>
{
    public void Configure(EntityTypeBuilder<WebhookProcessingAttempt> builder)
    {
        builder.ToTable("WebhookProcessingAttempts");
        builder.HasKey(attempt => attempt.Id);
        builder.HasIndex(attempt => new { attempt.Provider, attempt.ProviderEventId }).IsUnique();
        builder.HasIndex(attempt => new { attempt.Status, attempt.NextAttemptAt });
        builder.HasIndex(attempt => new { attempt.OrganizationId, attempt.ReceivedAt });
        builder.Property(attempt => attempt.Provider).HasMaxLength(50).IsRequired();
        builder.Property(attempt => attempt.ProviderEventId).HasMaxLength(200).IsRequired();
        builder.Property(attempt => attempt.EventType).HasMaxLength(100).IsRequired();
        builder.Property(attempt => attempt.ProviderResourceId).HasMaxLength(200).IsRequired();
        builder.Property(attempt => attempt.ResourceKind).HasMaxLength(50).IsRequired();
        builder.Property(attempt => attempt.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(attempt => attempt.Status).HasMaxLength(30).IsRequired();
        builder.Property(attempt => attempt.LastError).HasMaxLength(2_000);
    }
}
