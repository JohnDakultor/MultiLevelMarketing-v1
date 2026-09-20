using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.MessageType).HasMaxLength(500).IsRequired();
        builder.Property(message => message.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.CorrelationId).HasMaxLength(100);
        builder.Property(message => message.ClaimedBy).HasMaxLength(200);
        builder.Property(message => message.LastError).HasMaxLength(2_000);
        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.DeadLetteredAt,
            message.NextAttemptAt,
        });
        builder.HasIndex(message => new { message.OrganizationId, message.OccurredAt });
    }
}
