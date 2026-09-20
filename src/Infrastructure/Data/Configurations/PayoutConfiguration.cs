using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Payouts;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class PayoutAccountConfiguration : IEntityTypeConfiguration<PayoutAccount>
{
    public void Configure(EntityTypeBuilder<PayoutAccount> builder)
    {
        builder.ToTable("PayoutAccounts");
        builder.HasKey(account => account.Id);
        builder.HasIndex(account => new { account.OrganizationId, account.AgentId });
        builder.Property(account => account.Method).HasMaxLength(50);
        builder.Property(account => account.MaskedAccountData).HasMaxLength(100);
        builder.Property(account => account.ProtectedAccountName).HasMaxLength(2_000);
        builder.Property(account => account.ProtectedAccountNumber).HasMaxLength(2_000);
        builder.Property(account => account.BankCode).HasMaxLength(20);
        builder.Property(account => account.Rail).HasMaxLength(20);
        builder
            .HasOne<Agent>()
            .WithMany()
            .HasForeignKey(account => account.AgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PayoutRequestConfiguration : IEntityTypeConfiguration<PayoutRequest>
{
    public void Configure(EntityTypeBuilder<PayoutRequest> builder)
    {
        builder.ToTable("PayoutRequests");
        builder.HasKey(payout => payout.Id);
        builder.HasIndex(payout => new
        {
            payout.OrganizationId,
            payout.Status,
            payout.RequestedAt,
        });
        builder.HasIndex(payout => new
        {
            payout.OrganizationId,
            payout.AgentId,
            payout.RequestedAt,
            payout.Id,
        });
        builder
            .HasIndex(payout => payout.ProviderTransferId)
            .IsUnique()
            .HasFilter("\"ProviderTransferId\" IS NOT NULL");
        builder.Property(payout => payout.Amount).HasPrecision(18, 2);
        builder.Property(payout => payout.Currency).HasMaxLength(3);
        builder.Property(payout => payout.ProviderReference).HasMaxLength(200);
        builder.Property(payout => payout.ProviderBatchId).HasMaxLength(200);
        builder.Property(payout => payout.ProviderTransferId).HasMaxLength(200);
        builder.Property(payout => payout.FailureCode).HasMaxLength(100);
        builder.Property(payout => payout.FailureMessage).HasMaxLength(500);
        builder
            .HasOne<PayoutAccount>()
            .WithMany()
            .HasForeignKey(payout => payout.PayoutAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
