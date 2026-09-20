using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.Property(cart => cart.SessionId).HasMaxLength(128);
        builder.Property(cart => cart.ReferralCode).HasMaxLength(64);
        builder.Property(cart => cart.AttributionSource).HasMaxLength(50);

        builder
            .HasIndex(cart => new { cart.OrganizationId, cart.CustomerId })
            .IsUnique()
            .HasFilter("\"CustomerId\" IS NOT NULL");
        builder
            .HasIndex(cart => new { cart.OrganizationId, cart.SessionId })
            .IsUnique()
            .HasFilter("\"SessionId\" IS NOT NULL");

        builder
            .HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasIndex(item => new { item.CartId, item.ProductVariantId }).IsUnique();
    }
}
