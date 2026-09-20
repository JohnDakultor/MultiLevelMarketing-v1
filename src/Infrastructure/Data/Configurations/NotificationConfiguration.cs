using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Notifications;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder
            .Property(notification => notification.Kind)
            .HasConversion<string>()
            .HasMaxLength(64);
        builder
            .Property(notification => notification.DeliveryStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(notification => notification.TemplateKey).HasMaxLength(100).IsRequired();
        builder.Property(notification => notification.Culture).HasMaxLength(20).IsRequired();
        builder
            .Property(notification => notification.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(notification => notification.FailureCode).HasMaxLength(100);

        builder.OwnsOne(
            notification => notification.Content,
            owned =>
            {
                owned.Property(content => content.Title).HasColumnName("Title").HasMaxLength(150);
                owned
                    .Property(content => content.PlainTextBody)
                    .HasColumnName("PlainTextBody")
                    .HasMaxLength(4_000);
                owned
                    .Property(content => content.ActionPath)
                    .HasColumnName("ActionPath")
                    .HasMaxLength(2_048);
            }
        );

        builder
            .HasIndex(notification => new
            {
                notification.OrganizationId,
                notification.IdempotencyKey,
            })
            .IsUnique();
        builder.HasIndex(notification => new
        {
            notification.OrganizationId,
            notification.RecipientUserId,
            notification.CreatedAt,
            notification.Id,
        });
        builder.HasIndex(notification => new
        {
            notification.OrganizationId,
            notification.RecipientUserId,
            notification.ReadAt,
        });
        builder.HasIndex(notification => new
        {
            notification.DeliveryStatus,
            notification.CreatedAt,
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(notification => notification.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
