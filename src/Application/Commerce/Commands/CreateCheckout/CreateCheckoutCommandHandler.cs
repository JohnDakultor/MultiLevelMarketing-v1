using System.Text.Json;
using modular_mlm.Application.Commerce.Commands.CreateCheckout.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Referral;

namespace modular_mlm.Application.Commerce.Commands.CreateCheckout;

public sealed class CreateCheckoutCommandHandler(
    IApplicationDbContext db,
    IUser currentUser,
    TimeProvider clock,
    IInventoryReservationSettings reservationSettings
) : IRequestHandler<CreateCheckoutCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCheckoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var now = clock.GetUtcNow();

        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var customer = await db
            .CustomerProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.UserId == currentUserId,
                cancellationToken
            );
        if (customer is null)
            throw new KeyNotFoundException(
                "A customer profile was not found for the current user in this organization."
            );

        var cart = await db
            .Carts.Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.CustomerId == customer.Id,
                cancellationToken
            );
        if (cart is null)
            throw new KeyNotFoundException("The current customer does not have a cart.");
        if (cart.Items.Count == 0)
            throw new InvalidOperationException("The cart is empty.");

        var resolvedAttribution = await ResolveAttributionAsync(
            request.OrganizationId,
            customer.Id,
            cart,
            organization.Referrals,
            now,
            cancellationToken
        );

        var variantIds = cart.Items.Select(item => item.ProductVariantId).Distinct().ToArray();
        var variants = await db
            .ProductVariants.Where(variant => variantIds.Contains(variant.Id))
            .ToDictionaryAsync(variant => variant.Id, cancellationToken);
        if (variants.Count != variantIds.Length)
            throw new InvalidOperationException("One or more cart variants no longer exist.");

        var productIds = variants.Values.Select(variant => variant.ProductId).Distinct().ToArray();
        var products = await db
            .Products.AsNoTracking()
            .Where(product =>
                productIds.Contains(product.Id)
                && product.OrganizationId == request.OrganizationId
                && product.Status == ProductStatus.Active
            )
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        if (products.Count != productIds.Length)
            throw new InvalidOperationException(
                "One or more cart products are unavailable in this organization."
            );

        var commissionProfileIds = products
            .Values.Where(product => product.CommissionProfileId.HasValue)
            .Select(product => product.CommissionProfileId!.Value)
            .Distinct()
            .ToArray();
        var commissionProfiles = await db
            .ProductCommissionProfiles.AsNoTracking()
            .Where(profile =>
                profile.OrganizationId == request.OrganizationId
                && commissionProfileIds.Contains(profile.Id)
                && profile.EffectiveFrom <= now
                && (profile.EffectiveTo == null || profile.EffectiveTo > now)
            )
            .ToDictionaryAsync(profile => profile.Id, cancellationToken);

        var itemSnapshots = new List<CheckoutCartItemSnapshot>(cart.Items.Count);
        foreach (var cartItem in cart.Items)
        {
            var variant = variants[cartItem.ProductVariantId];
            if (!products.TryGetValue(variant.ProductId, out var product))
                throw new InvalidOperationException(
                    "A cart product is unavailable in this organization."
                );

            variant.ReserveStock(cartItem.Quantity);

            ProductCommissionProfile? commissionProfile = null;
            if (product.CommissionProfileId.HasValue)
                commissionProfiles.TryGetValue(
                    product.CommissionProfileId.Value,
                    out commissionProfile
                );

            itemSnapshots.Add(
                new CheckoutCartItemSnapshot(
                    product.Id,
                    variant.Id,
                    product.Name,
                    variant.Sku,
                    cartItem.Quantity,
                    variant.Price,
                    commissionProfile?.DirectSalesEligible == true
                        ? variant.Price * cartItem.Quantity
                        : 0m,
                    commissionProfile?.DirectSalesRateOverride,
                    commissionProfile?.BinaryVolumeEligible == true
                        ? (commissionProfile.BinaryVolumeOverride ?? variant.BusinessVolume)
                            * cartItem.Quantity
                        : 0m,
                    product.CommissionProfileId
                )
            );
        }

        var snapshot = new CheckoutCartSnapshot(
            cart.Id,
            request.OrganizationId,
            customer.Id,
            resolvedAttribution,
            organization.CurrencyCode,
            CreateAddressSnapshot(request.ShippingAddress),
            CreateAddressSnapshot(request.BillingAddress),
            itemSnapshots
        );
        var order = CreateOrder(snapshot, now);

        foreach (var cartItem in cart.Items)
        {
            var variant = variants[cartItem.ProductVariantId];
            if (!variant.StockKeepingEnabled)
                continue;
            db.InventoryReservations.Add(
                InventoryReservation.Reserve(
                    request.OrganizationId,
                    order.Id,
                    variant.Id,
                    cartItem.Quantity,
                    now,
                    now.Add(reservationSettings.Lifetime)
                )
            );
        }

        db.Carts.Remove(cart);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    private async Task<ReferralAttributionContext?> ResolveAttributionAsync(
        Guid organizationId,
        Guid customerId,
        Cart cart,
        ReferralSettings settings,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        ReferralAttributionContext? incomingAttribution = null;
        if (cart.AttributedAgentId.HasValue)
        {
            if (
                string.IsNullOrWhiteSpace(cart.ReferralCode)
                || !cart.ReferralCapturedAt.HasValue
                || !Enum.TryParse<AttributionSource>(
                    cart.AttributionSource,
                    ignoreCase: true,
                    out var source
                )
            )
                throw new InvalidOperationException("The cart referral attribution is invalid.");

            var agentIsActive = await db
                .Agents.AsNoTracking()
                .AnyAsync(
                    agent =>
                        agent.Id == cart.AttributedAgentId.Value
                        && agent.OrganizationId == organizationId
                        && agent.Status == AgentStatus.Active,
                    cancellationToken
                );
            if (agentIsActive)
                incomingAttribution = ReferralAttributionContext.Capture(
                    cart.AttributedAgentId.Value,
                    cart.ReferralCode,
                    cart.ReferralCapturedAt.Value,
                    source
                );
        }

        var storedAttribution = await db.ReferralAttributions.SingleOrDefaultAsync(
            attribution =>
                attribution.OrganizationId == organizationId
                && attribution.CustomerId == customerId,
            cancellationToken
        );
        ReferralAttributionContext? existingAttribution = null;
        if (storedAttribution is not null)
        {
            var existingAgentIsActive = await db
                .Agents.AsNoTracking()
                .AnyAsync(
                    agent =>
                        agent.Id == storedAttribution.AgentId
                        && agent.OrganizationId == organizationId
                        && agent.Status == AgentStatus.Active,
                    cancellationToken
                );
            if (existingAgentIsActive)
                existingAttribution = storedAttribution.ToContext();
        }

        var hasCompletedFirstPurchase = await db
            .Orders.AsNoTracking()
            .AnyAsync(
                order =>
                    order.OrganizationId == organizationId
                    && order.CustomerId == customerId
                    && (
                        order.PaymentStatus == PaymentStatus.Paid
                        || order.PaymentStatus == PaymentStatus.PartiallyRefunded
                        || order.PaymentStatus == PaymentStatus.Refunded
                    ),
                cancellationToken
            );
        var resolvedAttribution = ReferralAttributionPolicy.Resolve(
            settings,
            incomingAttribution,
            existingAttribution,
            hasCompletedFirstPurchase,
            now
        );

        if (incomingAttribution is not null && resolvedAttribution == incomingAttribution)
        {
            var expiresAt = incomingAttribution.CapturedAt.AddDays(settings.AttributionWindowDays);
            if (storedAttribution is null)
                db.ReferralAttributions.Add(
                    ReferralAttribution.Capture(
                        organizationId,
                        customerId,
                        incomingAttribution,
                        expiresAt
                    )
                );
            else
                storedAttribution.Recapture(incomingAttribution, expiresAt);
        }

        return resolvedAttribution;
    }

    private static Order CreateOrder(CheckoutCartSnapshot snapshot, DateTimeOffset now)
    {
        var order = Order.Create(
            snapshot.OrganizationId,
            $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            snapshot.CustomerId,
            snapshot.Attribution?.AgentId,
            snapshot.CurrencyCode,
            JsonSerializer.Serialize(snapshot.ShippingAddress),
            JsonSerializer.Serialize(snapshot.BillingAddress)
        );

        foreach (var item in snapshot.Items)
            order.AddItem(
                item.ProductId,
                item.ProductVariantId,
                item.ProductName,
                item.Sku,
                item.UnitPrice,
                item.Quantity,
                item.CommissionableAmount,
                item.DirectSalesRateOverride,
                item.BusinessVolume,
                item.CommissionProfileId
            );

        return order;
    }

    private static CheckoutAddressSnapshot CreateAddressSnapshot(CheckoutAddressInput input) =>
        new(
            input.RecipientName.Trim(),
            input.PhoneNumber.Trim(),
            input.AddressLine1.Trim(),
            NormalizeOptional(input.AddressLine2),
            NormalizeOptional(input.Barangay),
            input.CityOrMunicipality.Trim(),
            input.Province.Trim(),
            input.PostalCode.Trim(),
            input.CountryCode.Trim().ToUpperInvariant()
        );

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
