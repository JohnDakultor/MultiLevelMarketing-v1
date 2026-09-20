using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Commerce;

public sealed class CartItem : BaseAuditableEntity
{
    private CartItem() { }

    public Guid CartId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }

    internal static CartItem Create(Guid cartId, Guid variantId, int quantity) =>
        quantity <= 0
            ? throw new DomainInvariantException("Quantity must be positive.")
            : new CartItem
            {
                CartId = cartId,
                ProductVariantId = variantId,
                Quantity = quantity,
            };

    internal void Increase(int quantity)
    {
        if (quantity <= 0)
            throw new DomainInvariantException("Quantity must be positive.");
        checked
        {
            Quantity += quantity;
        }
    }

    internal void SetQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainInvariantException("Quantity must be positive.");
        Quantity = quantity;
    }
}
