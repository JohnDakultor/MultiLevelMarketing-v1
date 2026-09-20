using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Action).HasMaxLength(100).IsRequired();
        builder.Property(audit => audit.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(audit => audit.BeforeJson).HasColumnType("jsonb");
        builder.Property(audit => audit.AfterJson).HasColumnType("jsonb");
        builder.Property(audit => audit.Reason).HasMaxLength(1_000);
        builder.Property(audit => audit.IpAddress).HasMaxLength(45);
        builder.Property(audit => audit.UserAgent).HasMaxLength(1_024);
        builder.Property(audit => audit.TraceId).HasMaxLength(100);

        builder.HasIndex(audit => new { audit.OrganizationId, audit.CreatedAt });
        builder.HasIndex(audit => new
        {
            audit.OrganizationId,
            audit.EntityType,
            audit.EntityId,
        });
        builder.HasIndex(audit => new
        {
            audit.OrganizationId,
            audit.ActorUserId,
            audit.CreatedAt,
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(audit => audit.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetAfterSaveBehavior(PropertySaveBehavior.Throw);
        }
    }
}
