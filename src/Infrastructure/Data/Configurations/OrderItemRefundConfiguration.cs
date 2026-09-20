using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class OrderItemRefundConfiguration : IEntityTypeConfiguration<OrderItemRefund>
{
    public void Configure(EntityTypeBuilder<OrderItemRefund> builder)
    {
        builder.ToTable("OrderItemRefunds");
        builder.HasKey(x => x.Id);

        builder.Property(refund => refund.Quantity).HasPrecision(18, 4);
        builder.Property(refund => refund.RefundAmount).HasPrecision(18, 2);
        builder.Property(refund => refund.CommissionableAmountToReverse).HasPrecision(18, 2);
        builder.Property(refund => refund.BusinessVolumeToReverse).HasPrecision(18, 4);
        builder.Property(refund => refund.ReversalFailure).HasMaxLength(2_000);

        builder.HasIndex(refund => refund.PaymentRefundId).IsUnique();
        builder.HasIndex(refund => new
        {
            refund.OrganizationId,
            refund.OrderId,
            refund.RequestedAt,
        });
        builder.HasIndex(refund => new
        {
            refund.OrganizationId,
            refund.Status,
            refund.RequestedAt,
        });
        builder.HasIndex(refund => new
        {
            refund.OrganizationId,
            refund.OrderItemId,
            refund.Status,
        });

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(refund => refund.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<Order>()
            .WithMany()
            .HasForeignKey(refund => refund.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<OrderItem>()
            .WithMany()
            .HasForeignKey(refund => refund.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<PaymentRefund>()
            .WithMany()
            .HasForeignKey(refund => refund.PaymentRefundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
