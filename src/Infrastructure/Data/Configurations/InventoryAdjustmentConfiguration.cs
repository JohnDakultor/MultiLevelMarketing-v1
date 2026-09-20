using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("InventoryAdjustments");
        builder.HasKey(adjustment => adjustment.Id);

        builder
            .Property(adjustment => adjustment.AdjustmentType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder
            .Property(adjustment => adjustment.Reason)
            .HasMaxLength(InventoryAdjustment.MaximumReasonLength)
            .IsRequired();

        builder
            .Property(adjustment => adjustment.IdempotencyKey)
            .HasMaxLength(InventoryAdjustment.MaximumIdempotencyKeyLength)
            .IsRequired();

        builder.Property(adjustment => adjustment.ActorUserId).IsRequired();

        builder
            .HasIndex(adjustment => new { adjustment.OrganizationId, adjustment.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_InventoryAdjustments_Org_IdempotencyKey");

        builder
            .HasIndex(adjustment => new
            {
                adjustment.OrganizationId,
                adjustment.ProductVariantId,
                adjustment.OccurredAt,
                adjustment.Id,
            })
            .HasDatabaseName("IX_InventoryAdjustments_Org_Variant_Occurred");

        builder
            .HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(adjustment => adjustment.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_InventoryAdjustments_QuantityDelta_NonZero",
                "\"QuantityDelta\" <> 0"
            );
            table.HasCheckConstraint(
                "CK_InventoryAdjustments_Balances_NonNegative",
                "\"BalanceBefore\" >= 0 AND \"BalanceAfter\" >= 0"
            );
            table.HasCheckConstraint(
                "CK_InventoryAdjustments_BalanceEquation",
                "\"BalanceAfter\" = \"BalanceBefore\" + \"QuantityDelta\""
            );
        });
    }
}
