using System.Text.Json;

namespace modular_mlm.Application.Catalog.Queries.GetProductBySlug.Models;

public sealed record ProductVariantDetailDto(
    Guid Id,
    string Sku,
    decimal Price,
    decimal BusinessVolume,
    bool IsInStock,
    int? StockQuantity,
    decimal? Weight,
    JsonElement Attributes
);
