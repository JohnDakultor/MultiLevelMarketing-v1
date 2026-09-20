using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.Notifications;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class NotificationDeliveryEnvelopeConfiguration
    : IEntityTypeConfiguration<NotificationDeliveryEnvelope>
{
    public void Configure(EntityTypeBuilder<NotificationDeliveryEnvelope> builder)
    {
        builder.ToTable(
            "NotificationDeliveryEnvelopes",
            table =>
                table.HasCheckConstraint(
                    "CK_NotificationDeliveryEnvelopes_Source",
                    "(\"NotificationId\" IS NULL) <> (\"InvitationId\" IS NULL)"
                )
        );
        builder.HasKey(envelope => envelope.Id);
        builder.Property(envelope => envelope.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(envelope => envelope.TemplateKey).HasMaxLength(100).IsRequired();
        builder.Property(envelope => envelope.ProtectedPayload).HasColumnType("text").IsRequired();
        builder.Property(envelope => envelope.PayloadHash).HasMaxLength(64).IsFixedLength();
        builder.Property(envelope => envelope.ProviderIdempotencyKey).HasMaxLength(200);
        builder.Property(envelope => envelope.ClaimedBy).HasMaxLength(200);
        builder.Property(envelope => envelope.FailureCode).HasMaxLength(100);
        builder
            .HasIndex(envelope => new { envelope.NotificationId, envelope.Channel })
            .IsUnique()
            .HasFilter("\"NotificationId\" IS NOT NULL");
        builder
            .HasIndex(envelope => new { envelope.InvitationId, envelope.Channel })
            .IsUnique()
            .HasFilter("\"InvitationId\" IS NOT NULL");
        builder.HasIndex(envelope => new
        {
            envelope.DeliveredAt,
            envelope.DeadLetteredAt,
            envelope.NextAttemptAt,
        });
    }
}
