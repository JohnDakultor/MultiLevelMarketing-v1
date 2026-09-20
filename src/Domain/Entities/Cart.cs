using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Commerce;

public sealed class Cart : OrganizationEntity
{
    public const int MaximumQuantityPerLine = 100;

    private readonly List<CartItem> _items = [];

    private Cart() { }

    public Guid? CustomerId { get; private set; }
    public string? SessionId { get; private set; }
    public Guid? AttributedAgentId { get; private set; }
    public string? ReferralCode { get; private set; }
    public DateTimeOffset? ReferralCapturedAt { get; private set; }
    public string? AttributionSource { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public static Cart Create(Guid organizationId, Guid? customerId, string? sessionId)
    {
        if (
            organizationId == Guid.Empty
            || (customerId is null && string.IsNullOrWhiteSpace(sessionId))
        )
            throw new DomainInvariantException(
                "A cart requires an organization and customer or session."
            );
        return new Cart
        {
            OrganizationId = organizationId,
            CustomerId = customerId,
            SessionId = sessionId,
        };
    }

    public void AddItem(Guid variantId, int quantity)
    {
        if (variantId == Guid.Empty || quantity <= 0)
            throw new DomainInvariantException("Variant and positive quantity are required.");
        var existing = _items.SingleOrDefault(x => x.ProductVariantId == variantId);
        if ((existing?.Quantity ?? 0) + quantity > MaximumQuantityPerLine)
            throw new DomainInvariantException(
                $"A cart line cannot exceed {MaximumQuantityPerLine} items."
            );
        if (existing is null)
            _items.Add(CartItem.Create(Id, variantId, quantity));
        else
            existing.Increase(quantity);
    }

    public void UpdateItem(Guid variantId, int quantity)
    {
        var item =
            _items.SingleOrDefault(x => x.ProductVariantId == variantId)
            ?? throw new DomainInvariantException("Cart item was not found.");
        if (quantity > MaximumQuantityPerLine)
            throw new DomainInvariantException(
                $"A cart line cannot exceed {MaximumQuantityPerLine} items."
            );
        if (quantity <= 0)
            _items.Remove(item);
        else
            item.SetQuantity(quantity);
    }

    public void ApplyReferral(
        Guid agentId,
        string referralCode,
        DateTimeOffset capturedAt,
        string source
    )
    {
        if (_items.Count > 0 && AttributedAgentId is not null && AttributedAgentId != agentId)
            throw new DomainInvariantException(
                "Referral attribution cannot be replaced after it is established."
            );
        AttributedAgentId = agentId;
        ReferralCode = referralCode;
        ReferralCapturedAt = capturedAt;
        AttributionSource = source;
    }

    public void Clear() => _items.Clear();

    public void AssignToCustomer(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new DomainInvariantException("Customer is required.");
        if (CustomerId.HasValue && CustomerId != customerId)
            throw new DomainInvariantException("The cart already belongs to another customer.");

        CustomerId = customerId;
        SessionId = null;
    }

    public void MergeAnonymousCart(Cart anonymousCart)
    {
        ArgumentNullException.ThrowIfNull(anonymousCart);
        if (!CustomerId.HasValue || OrganizationId != anonymousCart.OrganizationId)
            throw new DomainInvariantException(
                "Only carts in the same organization can be merged."
            );
        if (anonymousCart.CustomerId.HasValue)
            throw new DomainInvariantException("The source cart must be anonymous.");

        foreach (var sourceItem in anonymousCart.Items)
        {
            var targetItem = _items.SingleOrDefault(item =>
                item.ProductVariantId == sourceItem.ProductVariantId
            );
            var mergedQuantity = Math.Min(
                MaximumQuantityPerLine,
                (targetItem?.Quantity ?? 0) + sourceItem.Quantity
            );

            if (targetItem is null)
                _items.Add(CartItem.Create(Id, sourceItem.ProductVariantId, mergedQuantity));
            else
                targetItem.SetQuantity(mergedQuantity);
        }

        if (AttributedAgentId is null && anonymousCart.AttributedAgentId.HasValue)
        {
            AttributedAgentId = anonymousCart.AttributedAgentId;
            ReferralCode = anonymousCart.ReferralCode;
            ReferralCapturedAt = anonymousCart.ReferralCapturedAt;
            AttributionSource = anonymousCart.AttributionSource;
        }
    }
}
