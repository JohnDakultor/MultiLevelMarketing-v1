using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Commerce.Commands.AddCartItem;

public sealed class AddCartItemCommandHandler(
    ICartSessionAccessor cartSessions,
    IApplicationDbContext db,
    IUser currentUser
) : IRequestHandler<AddCartItemCommand, Guid>
{
    public async Task<Guid> Handle(AddCartItemCommand request, CancellationToken cancellationToken)
    {
        var customerId = await ResolveCustomerIdAsync(request, cancellationToken);
        var sessionId = customerId.HasValue ? null : cartSessions.GetOrCreateSessionId();

        var variant = await (
            from candidate in db.ProductVariants.AsNoTracking()
            join product in db.Products.AsNoTracking() on candidate.ProductId equals product.Id
            where
                candidate.Id == request.ProductVariantId
                && product.OrganizationId == request.OrganizationId
                && product.Status == ProductStatus.Active
            select candidate
        ).SingleOrDefaultAsync(cancellationToken);

        if (variant is null)
            throw new KeyNotFoundException(
                "An active product variant was not found in this organization."
            );

        var cart = await FindCartAsync(
            request.OrganizationId,
            customerId,
            sessionId,
            cancellationToken
        );
        var isNewCart = cart is null;
        cart ??= Cart.Create(request.OrganizationId, customerId, sessionId);

        ValidateResultingQuantity(cart, variant, request.Quantity);
        cart.AddItem(request.ProductVariantId, request.Quantity);

        if (isNewCart)
            db.Carts.Add(cart);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return cart.Id;
        }
        catch (DbUpdateException) when (isNewCart)
        {
            DetachNewCart(cart);

            var existingCart = await FindCartAsync(
                request.OrganizationId,
                customerId,
                sessionId,
                cancellationToken
            );
            if (existingCart is null)
                throw;

            ValidateResultingQuantity(existingCart, variant, request.Quantity);
            existingCart.AddItem(request.ProductVariantId, request.Quantity);
            await db.SaveChangesAsync(cancellationToken);

            return existingCart.Id;
        }
    }

    private async Task<Guid?> ResolveCustomerIdAsync(
        AddCartItemCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(currentUser.Id))
            return null;

        var customerId = await db
            .CustomerProfiles.AsNoTracking()
            .Where(customer =>
                customer.OrganizationId == request.OrganizationId
                && customer.UserId == currentUser.Id
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

    private static void ValidateResultingQuantity(
        Cart cart,
        ProductVariant variant,
        int quantityToAdd
    )
    {
        var currentQuantity =
            cart.Items.SingleOrDefault(item => item.ProductVariantId == variant.Id)?.Quantity ?? 0;
        var resultingQuantity = checked(currentQuantity + quantityToAdd);

        if (resultingQuantity > Cart.MaximumQuantityPerLine)
            throw new InvalidOperationException(
                $"A cart line cannot exceed {Cart.MaximumQuantityPerLine} items."
            );

        if (variant.StockKeepingEnabled && resultingQuantity > variant.AvailableQuantity)
            throw new InvalidOperationException(
                $"Insufficient inventory. Available stock: {variant.AvailableQuantity}."
            );
    }

    private void DetachNewCart(Cart cart)
    {
        foreach (var item in cart.Items)
            db.CartItems.Entry(item).State = EntityState.Detached;

        db.Carts.Entry(cart).State = EntityState.Detached;
    }
}
