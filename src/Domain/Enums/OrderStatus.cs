namespace modular_mlm.Domain.Commerce;

public enum OrderStatus
{
    PendingPayment,
    Paid,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    PartiallyRefunded,
    Refunded,
}
