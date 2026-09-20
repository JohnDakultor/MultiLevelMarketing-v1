namespace modular_mlm.Domain.Events;

public record InventoryAdjustedEvent(
    Guid OrganizationId,
    Guid AdjustmentId,
    Guid ProductVariantId,
    int QuantityDelta,
    int BalanceBefore,
    int BalanceAfter,
    string Reason,
    DateTimeOffset OccurredAt
) : BaseEvent;
