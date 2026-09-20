using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Commerce.Queries.GetCart;

public sealed class GetCartQueryHandler(
    IApplicationDbContext db,
    ICartSessionAccessor cartSessions,
    IUser currentUser
) : IRequestHandler<GetCartQuery, CartDto>
{
    public async Task<CartDto> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        var currency = await db
            .Organizations.AsNoTracking()
            .Where(organization => organization.Id == request.OrganizationId)
            .Select(organization => organization.CurrencyCode)
            .SingleOrDefaultAsync(cancellationToken);

        if (currency is null)
            throw new KeyNotFoundException("Organization was not found.");

        var customerId = await ResolveCustomerIdAsync(request.OrganizationId, cancellationToken);

        string? sessionId = null;

        if (!customerId.HasValue)
        {
            sessionId = cartSessions.GetSessionId();

            if (sessionId is null)
                return EmptyCart(request.OrganizationId, null, currency);
        }

        var cart = customerId.HasValue
            ? await db
                .Carts.AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == customerId.Value
                )
                .Select(candidate => new
                {
                    candidate.Id,
                    candidate.CustomerId,
                    candidate.SessionId,
                    candidate.AttributedAgentId,
                    candidate.ReferralCode,
                })
                .SingleOrDefaultAsync(cancellationToken)
            : await db
                .Carts.AsNoTracking()
                .Where(candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == null
                    && candidate.SessionId == sessionId
                )
                .Select(candidate => new
                {
                    candidate.Id,
                    candidate.CustomerId,
                    candidate.SessionId,
                    candidate.AttributedAgentId,
                    candidate.ReferralCode,
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (cart is null)
            return EmptyCart(request.OrganizationId, customerId, currency);

        var items = await (
            from cartItem in db.CartItems.AsNoTracking()
            where cartItem.CartId == cart.Id

            join variant in db.ProductVariants.AsNoTracking()
                on cartItem.ProductVariantId equals variant.Id
                into variants
            from variant in variants.DefaultIfEmpty()

            join product in db.Products.AsNoTracking()
                on variant!.ProductId equals product.Id
                into products
            from product in products.DefaultIfEmpty()

            orderby product == null ? string.Empty : product.Name

            select new CartItemDto(
                cartItem.Id,
                product == null ? Guid.Empty : product.Id,
                product == null ? string.Empty : product.Slug,
                cartItem.ProductVariantId,
                product == null ? "Unavailable product" : product.Name,
                variant == null ? string.Empty : variant.Sku,
                product == null ? string.Empty : product.DefaultImageUrl ?? string.Empty,
                cartItem.Quantity,
                variant == null ? 0m : variant.Price,
                variant == null ? 0m : variant.Price * cartItem.Quantity,
                variant == null ? 0m : variant.BusinessVolume,
                product != null
                    && variant != null
                    && product.Status == ProductStatus.Active
                    && (
                        !variant.StockKeepingEnabled
                        || variant.StockQuantity - variant.ReservedQuantity >= cartItem.Quantity
                    ),
                GetStockStatus(product, variant, cartItem.Quantity)
            )
        ).ToListAsync(cancellationToken);

        return new CartDto(
            cart.Id,
            request.OrganizationId,
            cart.CustomerId,
            cart.SessionId is not null,
            cart.AttributedAgentId,
            cart.ReferralCode,
            items,
            currency,
            items.Sum(item => item.LineTotal),
            items.Sum(item => item.Quantity)
        );
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
                "A customer profile was not found for the current user."
            );
    }

    private static CartDto EmptyCart(Guid organizationId, Guid? customerId, string currency) =>
        new(null, organizationId, customerId, false, null, null, [], currency, 0m, 0);

    private static string GetStockStatus(
        Product? product,
        ProductVariant? variant,
        int requestedQuantity
    )
    {
        if (product is null || variant is null)
            return "removed";

        if (product.Status != ProductStatus.Active)
            return "unpublished";

        if (!variant.StockKeepingEnabled)
            return "available";

        if (variant.AvailableQuantity == 0)
            return "out_of_stock";

        if (variant.AvailableQuantity < requestedQuantity)
            return "insufficient_stock";

        return "available";
    }
}
