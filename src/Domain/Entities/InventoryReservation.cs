using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class InventoryReservation : OrganizationEntity
{
    private InventoryReservation() { }

    public Guid OrderId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public DateTimeOffset ReservedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public string? ReleaseReason { get; private set; }

    public int Version { get; private set; } = 1;

    public static InventoryReservation Reserve(
        Guid organizationId,
        Guid orderId,
        Guid productVariantId,
        int quantity,
        DateTimeOffset now,
        DateTimeOffset expiresAt
    )
    {
        if (organizationId == Guid.Empty)
            throw new DomainInvariantException("Organization is required.");

        if (orderId == Guid.Empty)
            throw new DomainInvariantException("Order is required.");

        if (productVariantId == Guid.Empty)
            throw new DomainInvariantException("Product variant is required.");

        if (quantity <= 0)
            throw new DomainInvariantException("Quantity must be positive.");

        if (expiresAt <= now)
            throw new DomainInvariantException("Expiration must be in the future.");

        var reservation = new InventoryReservation
        {
            OrganizationId = organizationId,
            OrderId = orderId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            Status = InventoryReservationStatus.Active,
            ReservedAt = now,
            ExpiresAt = expiresAt,
            Version = 1,
        };

        reservation.AddDomainEvent(
            new InventoryReservedEvent(
                reservation.OrganizationId,
                reservation.Id,
                reservation.OrderId,
                reservation.ProductVariantId,
                reservation.Quantity,
                reservation.ReservedAt,
                reservation.ExpiresAt
            )
        );

        return reservation;
    }

    public bool Finalize(DateTimeOffset now)
    {
        if (Status == InventoryReservationStatus.Finalized)
            return false;

        if (Status == InventoryReservationStatus.Released)
            throw new DomainInvariantException("Cannot finalize a released reservation.");

        Status = InventoryReservationStatus.Finalized;
        FinalizedAt = now;
        Version++;

        AddDomainEvent(
            new InventoryReservationFinalizedEvent(
                OrganizationId,
                Id,
                OrderId,
                ProductVariantId,
                Quantity,
                FinalizedAt.Value
            )
        );
        return true;
    }

    public bool Release(string reason, DateTimeOffset now)
    {
        if (Status == InventoryReservationStatus.Released)
            return false;

        if (Status == InventoryReservationStatus.Finalized)
            throw new DomainInvariantException("Cannot release a finalized reservation.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainInvariantException("Release reason is required.");

        Status = InventoryReservationStatus.Released;
        ReleasedAt = now;
        ReleaseReason = reason.Trim();
        Version++;

        AddDomainEvent(
            new InventoryReleasedEvent(
                OrganizationId,
                Id,
                OrderId,
                ProductVariantId,
                Quantity,
                ReleaseReason,
                ReleasedAt.Value
            )
        );
        return true;
    }

    public bool IsExpired(DateTimeOffset now) =>
        Status == InventoryReservationStatus.Active && ExpiresAt <= now;
}
