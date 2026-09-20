namespace modular_mlm.Domain.Events;

public sealed record InventoryReservationFinalizedEvent(
    Guid OrganizationId,
    Guid ReservationId,
    Guid OrderId,
    Guid ProductVariantId,
    int Quantity,
    DateTimeOffset FinalizedAt
) : BaseEvent;
