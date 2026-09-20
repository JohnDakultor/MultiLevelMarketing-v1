namespace modular_mlm.Domain.Events;

public sealed record InventoryReleasedEvent(
    Guid OrganizationId,
    Guid ReservationId,
    Guid OrderId,
    Guid ProductVariantId,
    int Quantity,
    string Reason,
    DateTimeOffset ReleasedAt
) : BaseEvent;
