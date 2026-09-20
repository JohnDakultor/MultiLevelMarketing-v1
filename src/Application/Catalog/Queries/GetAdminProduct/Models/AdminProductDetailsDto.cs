using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProduct.Models;

public sealed record AdminProductDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    ProductStatus Status,
    Guid CategoryId,
    string CategoryName,
    string? Brand,
    string? DefaultImageUrl,
    Guid? CommissionProfileId,
    string? CommissionProfileName,
    DateTimeOffset Created,
    DateTimeOffset LastModified,
    IReadOnlyList<AdminProductVariantDto> Variants
);
