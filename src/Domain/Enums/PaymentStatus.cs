namespace modular_mlm.Domain.Commerce;

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    PartiallyRefunded,
    Refunded,
}
