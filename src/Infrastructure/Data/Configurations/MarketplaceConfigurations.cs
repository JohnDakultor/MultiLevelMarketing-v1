using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Application.Common.Persistence;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Referral;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> b)
    {
        b.ToTable("Organizations");
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Slug).HasMaxLength(200);
        b.Property(x => x.CurrencyCode).HasMaxLength(3);
        b.Property(x => x.TimeZone).HasMaxLength(100);
        b.Property(x => x.Locale).HasMaxLength(20);
        b.Property(x => x.BrandingRevision).HasDefaultValue(1);
        b.Property(x => x.PublishedBrandingRevision).HasDefaultValue(0);
        b.OwnsOne(x => x.Branding);
        b.OwnsOne(x => x.PublishedBranding);
        b.Navigation(x => x.PublishedBranding).IsRequired(false);
        b.OwnsOne(x => x.Features);
        b.OwnsOne(
            x => x.Commerce,
            owned =>
            {
                owned.Property(x => x.AllowGuestCheckout).HasDefaultValue(true);
                owned.Property(x => x.RequireShippingAddress).HasDefaultValue(true);
                owned.Property(x => x.RequireBillingAddress).HasDefaultValue(true);
                owned.Property(x => x.InventoryReservationMinutes).HasDefaultValue(30);
            }
        );
        b.OwnsOne(x => x.Network);
        b.OwnsOne(
            organization => organization.Referrals,
            owned =>
            {
                owned.Property(x => x.AttributionWindowDays).HasDefaultValue(30);

                owned.Property(x => x.AllowReferralOverride).HasDefaultValue(false);

                owned.Property(x => x.ReferralLockAfterFirstPurchase).HasDefaultValue(true);
            }
        );
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasIndex(x => new { x.OrganizationId, x.Slug })
            .IsUnique()
            .HasDatabaseName(DatabaseConstraintNames.CategoryOrganizationSlug);
    }
}

public sealed class ReferralAttributionConfiguration : IEntityTypeConfiguration<ReferralAttribution>
{
    public void Configure(EntityTypeBuilder<ReferralAttribution> b)
    {
        b.ToTable("ReferralAttributions");
        b.HasIndex(x => new { x.OrganizationId, x.CustomerId }).IsUnique();
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.AgentId,
            x.CapturedAt,
        });
        b.Property(x => x.ReferralCode).HasMaxLength(64);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");
        b.HasIndex(x => new { x.OrganizationId, x.Slug }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200);
        b.HasMany(x => x.Variants)
            .WithOne()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.ToTable("ProductVariants");
        b.HasIndex(x => new { x.ProductId, x.Sku }).IsUnique();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.BusinessVolume).HasPrecision(18, 4);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.ReservedQuantity).HasDefaultValue(0);
        b.ToTable(table =>
            table.HasCheckConstraint(
                "CK_ProductVariants_StockQuantity_NonNegative",
                "\"StockQuantity\" >= 0"
            )
        );
        b.ToTable(table =>
            table.HasCheckConstraint(
                "CK_ProductVariants_ReservedQuantity",
                "\"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"StockQuantity\""
            )
        );
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems");
        b.Property(x => x.DirectSalesRateOverride).HasPrecision(9, 6);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        b.Property(x => x.ShippingCarrier).HasMaxLength(100);
        b.Property(x => x.TrackingNumber).HasMaxLength(200);
        b.Property(x => x.FulfillmentVersion).IsConcurrencyToken();
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.CustomerId,
            x.Created,
            x.Id,
        });
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.AttributedAgentId,
            x.Created,
            x.Id,
        });
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.PaymentStatus,
            x.Created,
            x.Id,
        });
    }
}

public sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> b)
    {
        b.ToTable("Agents");
        b.Property(x => x.PreferredLeg).HasConversion<int?>();
        b.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.ReferralCode }).IsUnique();
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.Status,
            x.JoinedAt,
        });
        b.HasIndex(x => new { x.OrganizationId, x.AgentCode });
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.SponsorAgentId,
            x.JoinedAt,
            x.Id,
        });
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.PlacementParentAgentId,
                x.PlacementSide,
            })
            .IsUnique()
            .HasFilter("\"PlacementParentAgentId\" IS NOT NULL");
    }
}

public sealed class PlacementClosureConfiguration : IEntityTypeConfiguration<PlacementClosure>
{
    public void Configure(EntityTypeBuilder<PlacementClosure> b)
    {
        b.ToTable("PlacementClosure");
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.AncestorAgentId,
                x.DescendantAgentId,
            })
            .IsUnique();
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.AncestorAgentId,
            x.Depth,
        });
    }
}

public sealed class CommissionPlanConfiguration : IEntityTypeConfiguration<CommissionPlan>
{
    public void Configure(EntityTypeBuilder<CommissionPlan> b)
    {
        b.ToTable("CommissionPlans");
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.Name,
                x.Version,
            })
            .IsUnique();
        b.Property(x => x.DirectSalesRate).HasPrecision(9, 6);
        b.Property(x => x.ConfigurationVersion).IsConcurrencyToken();
        b.OwnsOne(x => x.BinaryPairing);
    }
}

public sealed class FinancialPrecisionConfiguration
    : IEntityTypeConfiguration<CommissionTransaction>,
        IEntityTypeConfiguration<BinaryVolumeEntry>,
        IEntityTypeConfiguration<BinaryVolumeBalance>,
        IEntityTypeConfiguration<WalletEntry>
{
    public void Configure(EntityTypeBuilder<CommissionTransaction> b)
    {
        b.ToTable("CommissionTransactions");
        b.Property(x => x.BaseAmount).HasPrecision(18, 2);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.BeneficiaryAgentId,
                x.SourceOrderId,
                x.SourceOrderItemId,
                x.RuleId,
            })
            .IsUnique()
            .HasFilter("\"SourceOrderId\" IS NOT NULL");
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.BeneficiaryAgentId,
                x.PairingRunId,
                x.RuleId,
            })
            .IsUnique()
            .HasFilter("\"PairingRunId\" IS NOT NULL");
        b.HasOne<BinaryPairingRun>()
            .WithMany()
            .HasForeignKey(x => x.PairingRunId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReversalOfCommissionId, x.SourceOrderItemRefundId })
            .IsUnique()
            .HasFilter("\"SourceOrderItemRefundId\" IS NOT NULL");
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.BeneficiaryAgentId,
            x.Created,
            x.Id,
        });
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.Created,
            x.Id,
        }).HasDatabaseName("IX_CommissionTransactions_Org_Created_Id");
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.Type,
            x.Created,
            x.Id,
        }).HasDatabaseName("IX_CommissionTransactions_Org_Type_Created_Id");
    }

    public void Configure(EntityTypeBuilder<BinaryVolumeEntry> b)
    {
        b.ToTable("BinaryVolumeEntries");
        b.Property(x => x.Volume).HasPrecision(18, 4);
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.OwnerAgentId,
                x.SourceOrderItemId,
                x.EntryType,
            })
            .IsUnique()
            .HasFilter("\"SourceOrderItemId\" IS NOT NULL");
        b.HasIndex(x => new
            {
                x.OrganizationId,
                x.OwnerAgentId,
                x.PairingRunId,
                x.Side,
                x.EntryType,
            })
            .IsUnique()
            .HasFilter("\"PairingRunId\" IS NOT NULL");
        b.HasOne<BinaryPairingRun>()
            .WithMany()
            .HasForeignKey(x => x.PairingRunId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ReversalOfEntryId, x.SourceOrderItemRefundId })
            .IsUnique()
            .HasFilter("\"SourceOrderItemRefundId\" IS NOT NULL");
        b.HasIndex(x => new
        {
            x.OrganizationId,
            x.OwnerAgentId,
            x.EffectiveAt,
            x.Id,
        });
    }

    public void Configure(EntityTypeBuilder<BinaryVolumeBalance> b)
    {
        b.ToTable("BinaryVolumeBalances");
        b.HasIndex(x => new { x.OrganizationId, x.AgentId }).IsUnique();
        b.Property(x => x.LeftAvailable).HasPrecision(18, 4);
        b.Property(x => x.RightAvailable).HasPrecision(18, 4);
        b.Property(x => x.LeftLifetime).HasPrecision(18, 4);
        b.Property(x => x.RightLifetime).HasPrecision(18, 4);
        b.Property(x => x.Version).IsConcurrencyToken();
    }

    public void Configure(EntityTypeBuilder<WalletEntry> b)
    {
        b.ToTable("WalletEntries");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new
        {
            x.WalletId,
            x.Created,
            x.Id,
        });
        b.HasIndex(x => new { x.WalletId, x.Type })
            .HasDatabaseName("IX_WalletEntries_Wallet_Type");
        b.HasIndex(x => new
            {
                x.WalletId,
                x.SourceType,
                x.SourceId,
                x.Type,
            })
            .IsUnique()
            .HasFilter("\"ReversalOfEntryId\" IS NULL");
        b.HasIndex(x => new
            {
                x.ReversalOfEntryId,
                x.SourceId,
                x.Type,
            })
            .IsUnique()
            .HasFilter("\"ReversalOfEntryId\" IS NOT NULL");
    }
}

public sealed class AgentWalletConfiguration : IEntityTypeConfiguration<AgentWallet>
{
    public void Configure(EntityTypeBuilder<AgentWallet> b)
    {
        b.ToTable("AgentWallets");
        b.HasIndex(x => new { x.OrganizationId, x.AgentId }).IsUnique();
        b.Property(x => x.Currency).HasMaxLength(3);
    }
}
