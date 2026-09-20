public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSlug,
    Guid ProductVariantId,
    string ProductName,
    string Sku,
    string ImageUrl,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal BusinessVolumePerUnit,
    bool IsAvailable,
    string StockStatus
);
