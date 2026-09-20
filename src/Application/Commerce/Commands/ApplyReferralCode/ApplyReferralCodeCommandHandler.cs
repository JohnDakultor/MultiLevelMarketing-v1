using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Referral;

namespace modular_mlm.Application.Commerce.Commands.ApplyReferralCode;

public sealed class ApplyReferralCodeCommandHandler(
    IApplicationDbContext db,
    ICartSessionAccessor cartSessions,
    IUser currentUser,
    TimeProvider clock
) : IRequestHandler<ApplyReferralCodeCommand, CartDto>
{
    public async Task<CartDto> Handle(
        ApplyReferralCodeCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var customerId = await ResolveCustomerIdAsync(request.OrganizationId, cancellationToken);
        var sessionId = customerId.HasValue ? null : cartSessions.GetOrCreateSessionId();
        var cart = await FindCartAsync(
            request.OrganizationId,
            customerId,
            sessionId,
            cancellationToken
        );
        var isNewCart = cart is null;
        cart ??= Cart.Create(request.OrganizationId, customerId, sessionId);

        var referralCode = request.ReferralCode.Trim().ToUpperInvariant();
        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.ReferralCode == referralCode
                    && candidate.Status == AgentStatus.Active,
                cancellationToken
            );
        if (agent is null)
            throw new KeyNotFoundException(
                "An active Agent with that referral code was not found in this organization."
            );

        var now = clock.GetUtcNow();
        var incomingAttribution = ReferralAttributionContext.Capture(
            agent.Id,
            agent.ReferralCode,
            now,
            AttributionSource.Checkout
        );

        ReferralAttribution? storedAttribution = null;
        ReferralAttributionContext? existingAttribution = null;
        var hasCompletedFirstPurchase = false;

        if (customerId.HasValue)
        {
            storedAttribution = await db.ReferralAttributions.SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == customerId.Value,
                cancellationToken
            );

            if (storedAttribution is not null)
            {
                var existingAgentIsActive = await db
                    .Agents.AsNoTracking()
                    .AnyAsync(
                        candidate =>
                            candidate.Id == storedAttribution.AgentId
                            && candidate.OrganizationId == request.OrganizationId
                            && candidate.Status == AgentStatus.Active,
                        cancellationToken
                    );
                if (existingAgentIsActive)
                    existingAttribution = storedAttribution.ToContext();
            }

            hasCompletedFirstPurchase = await db
                .Orders.AsNoTracking()
                .AnyAsync(
                    order =>
                        order.OrganizationId == request.OrganizationId
                        && order.CustomerId == customerId.Value
                        && (
                            order.PaymentStatus == PaymentStatus.Paid
                            || order.PaymentStatus == PaymentStatus.PartiallyRefunded
                            || order.PaymentStatus == PaymentStatus.Refunded
                        ),
                    cancellationToken
                );
        }

        var resolvedAttribution = ReferralAttributionPolicy.Resolve(
            organization.Referrals,
            incomingAttribution,
            existingAttribution,
            hasCompletedFirstPurchase,
            now
        );
        if (resolvedAttribution != incomingAttribution)
            throw new InvalidOperationException(
                "The referral code cannot replace the customer's existing attribution."
            );

        cart.ApplyReferral(
            resolvedAttribution.AgentId,
            resolvedAttribution.ReferralCode,
            resolvedAttribution.CapturedAt,
            resolvedAttribution.Source.ToString()
        );

        if (isNewCart)
            db.Carts.Add(cart);

        if (customerId.HasValue)
        {
            var expiresAt = now.AddDays(organization.Referrals.AttributionWindowDays);
            if (storedAttribution is null)
                db.ReferralAttributions.Add(
                    ReferralAttribution.Capture(
                        request.OrganizationId,
                        customerId.Value,
                        resolvedAttribution,
                        expiresAt
                    )
                );
            else
                storedAttribution.Recapture(resolvedAttribution, expiresAt);
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ProjectCartAsync(cart, organization.CurrencyCode, cancellationToken);
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
        var carts = db.Carts.Include(candidate => candidate.Items);

        return customerId.HasValue
            ? carts.SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == organizationId
                    && candidate.CustomerId == customerId.Value,
                cancellationToken
            )
            : carts.SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == organizationId
                    && candidate.CustomerId == null
                    && candidate.SessionId == sessionId,
                cancellationToken
            );
    }

    private async Task<CartDto> ProjectCartAsync(
        Cart cart,
        string currency,
        CancellationToken cancellationToken
    )
    {
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
            cart.OrganizationId,
            cart.CustomerId,
            cart.CustomerId is null,
            cart.AttributedAgentId,
            cart.ReferralCode,
            items,
            currency,
            items.Sum(item => item.LineTotal),
            items.Sum(item => item.Quantity)
        );
    }

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
        return variant.AvailableQuantity < requestedQuantity ? "insufficient_stock" : "available";
    }
}
