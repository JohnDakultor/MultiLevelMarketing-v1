using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class InventoryAdjustment : OrganizationEntity
{
    public const int MaximumReasonLength = 500;
    public const int MaximumIdempotencyKeyLength = 128;

    private InventoryAdjustment() { }

    public Guid ProductVariantId { get; private set; }
    public int QuantityDelta { get; private set; }
    public InventoryAdjustmentType AdjustmentType { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public int ExpectedVersion { get; private set; }
    public int BalanceBefore { get; private set; }
    public int BalanceAfter { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid ActorUserId { get; private set; }

    public static InventoryAdjustment Record(
        Guid organizationId,
        Guid productVariantId,
        int quantityDelta,
        InventoryAdjustmentType adjustmentType,
        string reason,
        string idempotencyKey,
        int expectedVersion,
        int balanceBefore,
        int balanceAfter,
        DateTimeOffset occurredAt,
        Guid actorUserId
    )
    {
        if (organizationId == Guid.Empty || productVariantId == Guid.Empty)
            throw new DomainInvariantException("Organization and product variant are required.");
        if (quantityDelta == 0)
            throw new DomainInvariantException("QuantityDelta must be non-zero.");
        if (!Enum.IsDefined(adjustmentType))
            throw new DomainInvariantException("AdjustmentType is invalid.");
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new DomainInvariantException("IdempotencyKey is required.");
        if (idempotencyKey.Trim().Length > MaximumIdempotencyKeyLength)
            throw new DomainInvariantException("IdempotencyKey is too long.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainInvariantException("A reason is required.");
        if (reason.Trim().Length > MaximumReasonLength)
            throw new DomainInvariantException("Reason is too long.");
        if (expectedVersion < 0)
            throw new DomainInvariantException("ExpectedVersion cannot be negative.");
        if (balanceBefore < 0 || balanceAfter < 0)
            throw new DomainInvariantException("Inventory balances cannot be negative.");
        if (balanceAfter != checked(balanceBefore + quantityDelta))
            throw new DomainInvariantException("Inventory balances do not match the adjustment.");
        if (actorUserId == Guid.Empty)
            throw new DomainInvariantException("ActorUserId is required.");

        var adjustment = new InventoryAdjustment
        {
            OrganizationId = organizationId,
            ProductVariantId = productVariantId,
            QuantityDelta = quantityDelta,
            AdjustmentType = adjustmentType,
            Reason = reason.Trim(),
            IdempotencyKey = idempotencyKey.Trim().ToLowerInvariant(),
            ExpectedVersion = expectedVersion,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            OccurredAt = occurredAt,
            ActorUserId = actorUserId,
        };

        adjustment.AddDomainEvent(
            new InventoryAdjustedEvent(
                adjustment.OrganizationId,
                adjustment.Id,
                adjustment.ProductVariantId,
                adjustment.QuantityDelta,
                adjustment.BalanceBefore,
                adjustment.BalanceAfter,
                adjustment.Reason,
                adjustment.OccurredAt
            )
        );

        return adjustment;
    }
}
