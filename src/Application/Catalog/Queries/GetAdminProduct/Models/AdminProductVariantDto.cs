using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProduct.Models;

public sealed record AdminProductVariantDto(
    Guid Id,
    string Sku,
    ProductStatus Status,
    decimal Price,
    decimal BusinessVolume,
    decimal? Weight,
    string AttributesJson,
    bool StockKeepingEnabled,
    int OnHand,
    int Reserved,
    int? Available,
    int Version
);
