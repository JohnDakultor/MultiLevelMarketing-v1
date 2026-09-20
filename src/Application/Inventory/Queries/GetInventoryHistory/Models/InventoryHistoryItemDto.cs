using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Inventory.Queries.GetInventoryHistory.Models;

public sealed record InventoryHistoryItemDto(
    Guid Id,
    InventoryAdjustmentType AdjustmentType,
    int QuantityDelta,
    int BalanceBefore,
    int BalanceAfter,
    string Reason,
    Guid ActorUserId,
    DateTimeOffset OccurredAt
);
