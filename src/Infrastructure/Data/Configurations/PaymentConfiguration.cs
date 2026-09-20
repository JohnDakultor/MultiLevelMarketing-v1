using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(payment => payment.Id);
        builder.HasIndex(payment => new { payment.OrganizationId, payment.OrderId }).IsUnique();
        builder
            .HasIndex(payment => payment.ProviderCheckoutSessionId)
            .IsUnique()
            .HasFilter("\"ProviderCheckoutSessionId\" IS NOT NULL");
        builder.HasIndex(payment => new { payment.Provider, payment.IdempotencyKey }).IsUnique();
        builder.Property(payment => payment.Provider).HasMaxLength(50);
        builder.Property(payment => payment.IdempotencyKey).HasMaxLength(255);
        builder.Property(payment => payment.ProviderCheckoutSessionId).HasMaxLength(200);
        builder.Property(payment => payment.ProviderPaymentId).HasMaxLength(200);
        builder
            .Property(payment => payment.CheckoutUrl)
            .HasConversion(
                uri => uri == null ? null : uri.AbsoluteUri,
                value => value == null ? null : new Uri(value)
            )
            .HasMaxLength(2_000);
        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.RefundedAmount).HasPrecision(18, 2);
        builder.Property(payment => payment.Currency).HasMaxLength(3);
        builder.Property(payment => payment.FailureCode).HasMaxLength(100);
        builder.Property(payment => payment.FailureMessage).HasMaxLength(500);
        builder
            .HasOne<Order>()
            .WithMany()
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.ToTable("PaymentRefunds");
        builder.HasKey(refund => refund.Id);
        builder.HasIndex(refund => refund.IdempotencyKey).IsUnique();
        builder
            .HasIndex(refund => refund.ProviderRefundId)
            .IsUnique()
            .HasFilter("\"ProviderRefundId\" IS NOT NULL");
        builder.Property(refund => refund.Amount).HasPrecision(18, 2);
        builder.Property(refund => refund.Currency).HasMaxLength(3);
        builder.Property(refund => refund.Reason).HasMaxLength(500);
        builder.Property(refund => refund.IdempotencyKey).HasMaxLength(255);
        builder.Property(refund => refund.ProviderRefundId).HasMaxLength(200);
        builder.Property(refund => refund.FailureMessage).HasMaxLength(500);
        builder
            .HasOne<Payment>()
            .WithMany()
            .HasForeignKey(refund => refund.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentWebhookReceiptConfiguration
    : IEntityTypeConfiguration<PaymentWebhookReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookReceipt> builder)
    {
        builder.ToTable("PaymentWebhookReceipts");
        builder.HasKey(receipt => receipt.Id);
        builder.HasIndex(receipt => new { receipt.Provider, receipt.ProviderEventId }).IsUnique();
        builder.Property(receipt => receipt.Provider).HasMaxLength(50);
        builder.Property(receipt => receipt.ProviderEventId).HasMaxLength(200);
        builder.Property(receipt => receipt.EventType).HasMaxLength(100);
        builder.Property(receipt => receipt.PayloadHash).HasMaxLength(128);
    }
}
