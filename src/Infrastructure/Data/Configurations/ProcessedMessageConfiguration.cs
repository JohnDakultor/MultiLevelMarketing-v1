using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");
        builder.HasKey(message => new { message.MessageId, message.ConsumerName });
        builder.Property(message => message.ConsumerName).HasMaxLength(200);
        builder.HasIndex(message => message.ProcessedAt);
    }
}
