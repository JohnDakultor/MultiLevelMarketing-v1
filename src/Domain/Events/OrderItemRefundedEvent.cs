namespace modular_mlm.Domain.Events;

public sealed record OrderItemRefundedEvent(
    Guid OrganizationId,
    Guid OrderId,
    Guid OrderItemId,
    Guid OrderItemRefundId,
    Guid PaymentRefundId
) : BaseEvent;
