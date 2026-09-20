using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class InventoryReservationConfiguration
    : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable(
            "InventoryReservations",
            table =>
                table.HasCheckConstraint("CK_InventoryReservations_Quantity", "\"Quantity\" > 0")
        );
        builder.HasKey(reservation => reservation.Id);
        builder
            .Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(reservation => reservation.ReleaseReason).HasMaxLength(500);
        builder.Property(reservation => reservation.Version).IsConcurrencyToken();
        builder
            .HasIndex(reservation => new
            {
                reservation.OrganizationId,
                reservation.OrderId,
                reservation.ProductVariantId,
            })
            .IsUnique();
        builder.HasIndex(reservation => new
        {
            reservation.OrganizationId,
            reservation.Status,
            reservation.ExpiresAt,
        });
        builder
            .HasOne<Order>()
            .WithMany()
            .HasForeignKey(reservation => reservation.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(reservation => reservation.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
