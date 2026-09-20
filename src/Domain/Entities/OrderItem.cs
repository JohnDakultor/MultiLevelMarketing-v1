using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Commerce;

public sealed class OrderItem : BaseAuditableEntity
{
    private OrderItem() { }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public string SkuSnapshot { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal CommissionableAmount { get; private set; }
    public decimal? DirectSalesRateOverride { get; private set; }
    public decimal BusinessVolume { get; private set; }
    public Guid? CommissionProfileId { get; private set; }
    public FulfillmentStatus FulfillmentStatus { get; private set; }

    internal static OrderItem Create(
        Guid orderId,
        Guid productId,
        Guid variantId,
        string productName,
        string sku,
        decimal unitPrice,
        int quantity,
        decimal commissionableAmount,
        decimal? directSalesRateOverride,
        decimal businessVolume,
        Guid? profileId
    )
    {
        if (
            unitPrice < 0
            || quantity <= 0
            || commissionableAmount < 0
            || directSalesRateOverride is < 0 or > 1
            || businessVolume < 0
        )
            throw new DomainInvariantException("Order item values are invalid.");
        return new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            ProductVariantId = variantId,
            ProductNameSnapshot = productName,
            SkuSnapshot = sku,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = unitPrice * quantity,
            CommissionableAmount = commissionableAmount,
            DirectSalesRateOverride = directSalesRateOverride,
            BusinessVolume = businessVolume,
            CommissionProfileId = profileId,
            FulfillmentStatus = FulfillmentStatus.Unfulfilled,
        };
    }

    public void StartFulfillment()
    {
        if (FulfillmentStatus != FulfillmentStatus.Unfulfilled)
            throw new DomainInvariantException("Item is already being fulfilled.");
        FulfillmentStatus = FulfillmentStatus.Processing;
    }

    public void MarkShipped()
    {
        if (FulfillmentStatus != FulfillmentStatus.Processing)
            throw new DomainInvariantException("Item must be processing before shipment.");
        FulfillmentStatus = FulfillmentStatus.Shipped;
    }

    internal void MarkDelivered()
    {
        if (FulfillmentStatus != FulfillmentStatus.Shipped)
            throw new DomainInvariantException("Item must be shipped before delivery.");
        FulfillmentStatus = FulfillmentStatus.Delivered;
    }

    internal void Cancel()
    {
        if (FulfillmentStatus != FulfillmentStatus.Unfulfilled)
            throw new DomainInvariantException("Only an unfulfilled order item can be cancelled.");
        FulfillmentStatus = FulfillmentStatus.Cancelled;
    }
}
