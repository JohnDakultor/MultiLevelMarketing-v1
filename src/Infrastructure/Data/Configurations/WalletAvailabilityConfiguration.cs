using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class WalletAvailabilityConfiguration
    : IEntityTypeConfiguration<WalletSettings>,
        IEntityTypeConfiguration<WalletEntry>,
        IEntityTypeConfiguration<CommissionTransaction>
{
    public void Configure(EntityTypeBuilder<WalletSettings> builder)
    {
        builder.ToTable(
            "WalletSettings",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_WalletSettings_ReleaseDelayDays_NonNegative",
                    "\"ReleaseDelayDays\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletSettings_ReturnWindowDays_NonNegative",
                    "\"ReturnWindowDays\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletSettings_MinimumPayoutAmount_Positive",
                    "\"MinimumPayoutAmount\" > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletSettings_MaximumNegativeBalance_NonNegative",
                    "\"MaximumNegativeBalance\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletSettings_CommissionReleaseTrigger_Valid",
                    "\"CommissionReleaseTrigger\" IN (0, 1, 2)"
                );
            }
        );

        builder.HasKey(settings => settings.OrganizationId);

        builder
            .HasOne<Organization>()
            .WithOne(organization => organization.Wallet)
            .HasForeignKey<WalletSettings>(settings => settings.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(settings => settings.CommissionReleaseTrigger)
            .HasConversion<int>()
            .HasDefaultValue(CommissionReleaseTrigger.PaymentConfirmed)
            .IsRequired();

        builder.Property(settings => settings.ReleaseDelayDays).HasDefaultValue(0).IsRequired();

        builder.Property(settings => settings.ReturnWindowDays).HasDefaultValue(0).IsRequired();

        builder
            .Property(settings => settings.MinimumPayoutAmount)
            .HasPrecision(18, 2)
            .HasDefaultValue(10.00m)
            .IsRequired();

        builder
            .Property(settings => settings.AllowNegativeRecoverableBalance)
            .HasDefaultValue(false)
            .IsRequired();

        builder
            .Property(settings => settings.MaximumNegativeBalance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<WalletEntry> builder)
    {
        builder.ToTable(
            "WalletEntries",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_WalletEntries_Release_IsAvailableCredit",
                    "\"ReleasedFromEntryId\" IS NULL OR \"Type\" = 1"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletEntries_Release_DoesNotReferenceSelf",
                    "\"ReleasedFromEntryId\" IS NULL OR \"ReleasedFromEntryId\" <> \"Id\""
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletEntries_Adjustment_HasIdempotencyKey",
                    "\"Type\" <> 5 OR \"IdempotencyKey\" IS NOT NULL"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletEntries_NonAdjustment_HasNoIdempotencyKey",
                    "\"Type\" = 5 OR \"IdempotencyKey\" IS NULL"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_WalletEntries_IdempotencyKey_NotBlank",
                    "\"IdempotencyKey\" IS NULL OR length(btrim(\"IdempotencyKey\")) > 0"
                );
            }
        );

        builder.Property(entry => entry.IdempotencyKey).HasMaxLength(128);

        builder
            .HasOne<WalletEntry>()
            .WithMany()
            .HasForeignKey(entry => entry.ReleasedFromEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(entry => entry.ReleasedFromEntryId)
            .IsUnique()
            .HasFilter("\"ReleasedFromEntryId\" IS NOT NULL");

        builder.HasIndex(entry => new { entry.SourceId, entry.Type });

        builder
            .HasIndex(entry => new { entry.WalletId, entry.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }

    public void Configure(EntityTypeBuilder<CommissionTransaction> builder)
    {
        builder.HasIndex(commission => new
        {
            commission.OrganizationId,
            commission.Status,
            commission.Created,
            commission.Id,
        });
    }
}
