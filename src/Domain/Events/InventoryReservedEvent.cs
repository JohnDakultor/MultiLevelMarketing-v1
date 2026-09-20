namespace modular_mlm.Domain.Events;

public sealed record InventoryReservedEvent(
    Guid OrganizationId,
    Guid ReservationId,
    Guid OrderId,
    Guid ProductVariantId,
    int Quantity,
    DateTimeOffset ReservedAt,
    DateTimeOffset ExpiresAt
) : BaseEvent;
