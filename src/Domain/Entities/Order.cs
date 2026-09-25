using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Commerce;

public sealed class Order : OrganizationEntity
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid? AttributedAgentId { get; private set; }
    public OrderStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public string? ShippingCarrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public long FulfillmentVersion { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal ShippingTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string ShippingAddressJson { get; private set; } = "{}";
    public string BillingAddressJson { get; private set; } = "{}";
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        Guid organizationId,
        string orderNumber,
        Guid customerId,
        Guid? attributedAgentId,
        string currency,
        string shippingAddressJson,
        string billingAddressJson
    )
    {
        if (
            organizationId == Guid.Empty
            || customerId == Guid.Empty
            || string.IsNullOrWhiteSpace(orderNumber)
        )
            throw new DomainInvariantException(
                "Organization, customer, and order number are required."
            );
        if (currency.Length != 3)
            throw new DomainInvariantException("Currency must be a three-letter ISO code.");
        return new Order
        {
            OrganizationId = organizationId,
            OrderNumber = orderNumber,
            CustomerId = customerId,
            AttributedAgentId = attributedAgentId,
            Currency = currency.ToUpperInvariant(),
            ShippingAddressJson = shippingAddressJson,
            BillingAddressJson = billingAddressJson,
            Status = OrderStatus.PendingPayment,
            PaymentStatus = PaymentStatus.Pending,
        };
    }

    public void AddItem(
        Guid productId,
        Guid variantId,
        string productName,
        string sku,
        decimal unitPrice,
        int quantity,
        decimal commissionableAmount,
        decimal? directSalesRateOverride,
        decimal businessVolume,
        Guid? commissionProfileId
    )
    {
        if (Status != OrderStatus.PendingPayment)
            throw new DomainInvariantException(
                "Items cannot be changed after payment processing starts."
            );
        _items.Add(
            OrderItem.Create(
                Id,
                productId,
                variantId,
                productName,
                sku,
                unitPrice,
                quantity,
                commissionableAmount,
                directSalesRateOverride,
                businessVolume,
                commissionProfileId
            )
        );
        RecalculateTotals();
    }

    public void SetCharges(decimal discount, decimal shipping, decimal tax)
    {
        if (discount < 0 || shipping < 0 || tax < 0 || discount > Subtotal)
            throw new DomainInvariantException("Order charges are invalid.");
        DiscountTotal = discount;
        ShippingTotal = shipping;
        TaxTotal = tax;
        RecalculateTotals();
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        if (_items.Count == 0 || PaymentStatus == PaymentStatus.Paid)
            throw new DomainInvariantException("Order cannot be paid.");
        PaymentStatus = PaymentStatus.Paid;
        Status = OrderStatus.Paid;
        PaidAt = paidAt;
        FulfillmentVersion++;
        AddDomainEvent(new OrderPaidEvent(OrganizationId, Id));
    }

    public void StartProcessing()
    {
        if (Status != OrderStatus.Paid)
            throw new DomainInvariantException("Only paid orders can be processed.");

        foreach (
            var item in _items.Where(item =>
                item.FulfillmentStatus == FulfillmentStatus.Unfulfilled
            )
        )
            item.StartFulfillment();

        Status = OrderStatus.Processing;
        FulfillmentVersion++;
    }

    public void Ship() => Ship(DateTimeOffset.UtcNow);

    public void Ship(
        DateTimeOffset shippedAt,
        string? shippingCarrier = null,
        string? trackingNumber = null
    )
    {
        if (Status != OrderStatus.Processing)
            throw new DomainInvariantException("Only processing orders can ship.");

        var normalizedCarrier = string.IsNullOrWhiteSpace(shippingCarrier)
            ? null
            : shippingCarrier.Trim();
        var normalizedTrackingNumber = string.IsNullOrWhiteSpace(trackingNumber)
            ? null
            : trackingNumber.Trim();
        if ((normalizedCarrier is null) != (normalizedTrackingNumber is null))
            throw new DomainInvariantException(
                "Shipping carrier and tracking number must be supplied together."
            );

        foreach (
            var item in _items.Where(item => item.FulfillmentStatus == FulfillmentStatus.Processing)
        )
            item.MarkShipped();

        Status = OrderStatus.Shipped;
        ShippedAt = shippedAt;
        ShippingCarrier = normalizedCarrier;
        TrackingNumber = normalizedTrackingNumber;
        FulfillmentVersion++;
    }

    public void Deliver(DateTimeOffset deliveredAt)
    {
        if (Status != OrderStatus.Shipped)
            throw new DomainInvariantException("Only shipped orders can be delivered.");

        foreach (
            var item in _items.Where(item => item.FulfillmentStatus == FulfillmentStatus.Shipped)
        )
            item.MarkDelivered();

        Status = OrderStatus.Delivered;
        DeliveredAt = deliveredAt;
        FulfillmentVersion++;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.PendingPayment || PaymentStatus != PaymentStatus.Pending)
            throw new DomainInvariantException("Only an unpaid pending order can be cancelled.");
        if (_items.Any(item => item.FulfillmentStatus != FulfillmentStatus.Unfulfilled))
            throw new DomainInvariantException(
                "An order cannot be cancelled after fulfillment has started."
            );

        foreach (var item in _items)
            item.Cancel();

        Status = OrderStatus.Cancelled;
        FulfillmentVersion++;
    }

    public void Refund(bool partial)
    {
        if (PaymentStatus != PaymentStatus.Paid && PaymentStatus != PaymentStatus.PartiallyRefunded)
            throw new DomainInvariantException("Only paid orders can be refunded.");
        PaymentStatus = partial ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded;
        Status = partial ? OrderStatus.PartiallyRefunded : OrderStatus.Refunded;
        FulfillmentVersion++;
        AddDomainEvent(new OrderRefundedEvent(Id));
    }

    private void RecalculateTotals()
    {
        Subtotal = _items.Sum(x => x.LineTotal);
        GrandTotal = Subtotal - DiscountTotal + ShippingTotal + TaxTotal;
    }
}
