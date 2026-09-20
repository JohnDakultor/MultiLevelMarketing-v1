using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Idempotency;
using modular_mlm.Infrastructure.Idempotency;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable(
            "IdempotencyRecords",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_IdempotencyRecords_ExpiresAfterStart",
                    "\"ExpiresAt\" > \"StartedAt\""
                );
                tableBuilder.HasCheckConstraint(
                    "CK_IdempotencyRecords_CompletedState",
                    "(\"Status\" = 'Completed' AND \"CompletedAt\" IS NOT NULL AND \"StatusCode\" IS NOT NULL) OR "
                        + "(\"Status\" <> 'Completed' AND \"CompletedAt\" IS NULL AND \"StatusCode\" IS NULL AND \"ProtectedOutcome\" IS NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_IdempotencyRecords_StatusCode",
                    "\"StatusCode\" IS NULL OR (\"StatusCode\" >= 100 AND \"StatusCode\" <= 599)"
                );
            }
        );

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Scope).HasMaxLength(200).IsRequired();
        builder.Property(record => record.KeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder
            .Property(record => record.RequestHash)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();
        builder
            .Property(record => record.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(record => record.ProtectedOutcome).HasColumnType("text");
        builder.Property(record => record.Version).IsConcurrencyToken();

        builder
            .HasIndex(record => new
            {
                record.OrganizationId,
                record.Scope,
                record.KeyHash,
            })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NOT NULL")
            .HasDatabaseName("UX_IdempotencyRecords_Tenant_Scope_KeyHash");
        builder
            .HasIndex(record => new { record.Scope, record.KeyHash })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NULL")
            .HasDatabaseName("UX_IdempotencyRecords_Global_Scope_KeyHash");
        builder.HasIndex(record => new { record.Status, record.ExpiresAt });
        builder.HasIndex(record => new { record.Status, record.StartedAt });
    }
}
