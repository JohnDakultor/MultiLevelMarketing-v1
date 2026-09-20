namespace modular_mlm.Application.Inventory.Queries.GetInventory.Models;

public sealed record InventoryPageDto(
    IReadOnlyList<InventoryItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
