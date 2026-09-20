using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProducts.Models;

public sealed record AdminProductDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    ProductStatus Status,
    decimal Price,
    decimal BusinessVolume,
    int StockQuantity
);
