namespace modular_mlm.Domain.Commerce;

public enum OrderPaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    PartiallyRefunded,
    Refunded,
}
