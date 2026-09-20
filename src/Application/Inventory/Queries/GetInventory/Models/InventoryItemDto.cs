using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Inventory.Queries.GetInventory.Models;

public sealed record InventoryItemDto(
    Guid ProductId,
    string ProductName,
    ProductStatus ProductStatus,
    Guid ProductVariantId,
    string Sku,
    ProductStatus VariantStatus,
    bool StockKeepingEnabled,
    int OnHand,
    int Reserved,
    int? Available,
    int Version,
    DateTimeOffset LastModified
);
