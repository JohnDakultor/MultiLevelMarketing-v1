using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetProductBySlug.Models;

public sealed record ProductDetailDto(
    Guid Id,
    Guid OrganizationId,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string Slug,
    string Description,
    string? Brand,
    string? DefaultImageUrl,
    string CurrencyCode,
    ProductStatus Status,
    IReadOnlyList<ProductVariantDetailDto> Variants
);
