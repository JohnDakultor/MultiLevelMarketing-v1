namespace modular_mlm.Application.Inventory.Queries.GetInventoryHistory.Models;

public sealed record InventoryHistoryPageDto(
    Guid ProductVariantId,
    IReadOnlyList<InventoryHistoryItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
