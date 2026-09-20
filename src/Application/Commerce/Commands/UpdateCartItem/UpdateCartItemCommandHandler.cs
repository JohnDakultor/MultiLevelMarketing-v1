using modular_mlm.Application.Commerce.Queries.GetCart;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Commands.UpdateCartItem;

public sealed class UpdateCartItemCommandHandler(
    IApplicationDbContext db,
    ICartSessionAccessor cartSessions,
    IUser currentUser,
    ISender sender
) : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    public async Task<CartDto> Handle(
        UpdateCartItemCommand request,
        CancellationToken cancellationToken
    )
    {
        var customerId = await ResolveCustomerIdAsync(request.OrganizationId, cancellationToken);
        var sessionId = customerId.HasValue ? null : cartSessions.GetSessionId();
        if (!customerId.HasValue && sessionId is null)
            throw new KeyNotFoundException("Cart item was not found.");

        var cart = await FindCartAsync(
            request.OrganizationId,
            customerId,
            sessionId,
            cancellationToken
        );
        var cartItem = cart?.Items.SingleOrDefault(item => item.Id == request.CartItemId);
        if (cart is null || cartItem is null)
            throw new KeyNotFoundException("Cart item was not found.");

        if (request.Quantity > 0)
        {
            var variant = await (
                from candidate in db.ProductVariants.AsNoTracking()
                join product in db.Products.AsNoTracking() on candidate.ProductId equals product.Id
                where
                    candidate.Id == cartItem.ProductVariantId
                    && product.OrganizationId == request.OrganizationId
                    && product.Status == ProductStatus.Active
                select candidate
            ).SingleOrDefaultAsync(cancellationToken);

            if (variant is null)
                throw new InvalidOperationException(
                    "The product variant is no longer available. Remove it from the cart."
                );
            if (variant.StockKeepingEnabled && variant.AvailableQuantity < request.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient inventory. Available stock: {variant.AvailableQuantity}."
                );
        }

        cart.UpdateItem(cartItem.ProductVariantId, request.Quantity);
        await db.SaveChangesAsync(cancellationToken);
        return await sender.Send(new GetCartQuery(request.OrganizationId), cancellationToken);
    }

    private async Task<Guid?> ResolveCustomerIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id))
            return null;

        var customerId = await db
            .CustomerProfiles.AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == organizationId && customer.UserId == currentUser.Id
            )
            .Select(customer => (Guid?)customer.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return customerId
            ?? throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );
    }

    private Task<Cart?> FindCartAsync(
        Guid organizationId,
        Guid? customerId,
        string? sessionId,
        CancellationToken cancellationToken
    )
    {
        var carts = db.Carts.Include(cart => cart.Items);
        return customerId.HasValue
            ? carts.SingleOrDefaultAsync(
                cart =>
                    cart.OrganizationId == organizationId && cart.CustomerId == customerId.Value,
                cancellationToken
            )
            : carts.SingleOrDefaultAsync(
                cart =>
                    cart.OrganizationId == organizationId
                    && cart.CustomerId == null
                    && cart.SessionId == sessionId,
                cancellationToken
            );
    }
}
