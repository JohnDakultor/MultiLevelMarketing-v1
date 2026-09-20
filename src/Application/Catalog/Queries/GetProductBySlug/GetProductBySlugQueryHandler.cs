using System.Text.Json;
using modular_mlm.Application.Catalog.Queries.GetProductBySlug.Models;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Queries.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductBySlugQuery, ProductDetailDto>
{
    public async Task<ProductDetailDto> Handle(
        GetProductBySlugQuery request,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        var product = await (
            from candidate in db.Products.AsNoTracking()
            join category in db.Categories.AsNoTracking() on candidate.CategoryId equals category.Id
            join organization in db.Organizations.AsNoTracking()
                on candidate.OrganizationId equals organization.Id
            where
                candidate.OrganizationId == request.OrganizationId
                && category.OrganizationId == request.OrganizationId
                && candidate.Slug == normalizedSlug
                && candidate.Status == ProductStatus.Active
            select new
            {
                candidate.Id,
                candidate.OrganizationId,
                candidate.CategoryId,
                CategoryName = category.Name,
                candidate.Name,
                candidate.Slug,
                candidate.Description,
                candidate.Brand,
                candidate.DefaultImageUrl,
                organization.CurrencyCode,
                candidate.Status,
            }
        ).SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            throw new KeyNotFoundException("Active product was not found.");

        var variants = await db
            .ProductVariants.AsNoTracking()
            .Where(variant => variant.ProductId == product.Id)
            .OrderBy(variant => variant.Price)
            .ThenBy(variant => variant.Sku)
            .Select(variant => new
            {
                variant.Id,
                variant.Sku,
                variant.Price,
                variant.BusinessVolume,
                variant.StockKeepingEnabled,
                variant.StockQuantity,
                variant.ReservedQuantity,
                variant.Weight,
                variant.AttributesJson,
            })
            .ToListAsync(cancellationToken);

        return new ProductDetailDto(
            product.Id,
            product.OrganizationId,
            product.CategoryId,
            product.CategoryName,
            product.Name,
            product.Slug,
            product.Description,
            product.Brand,
            product.DefaultImageUrl,
            product.CurrencyCode,
            product.Status,
            variants
                .Select(variant => new ProductVariantDetailDto(
                    variant.Id,
                    variant.Sku,
                    variant.Price,
                    variant.BusinessVolume,
                    !variant.StockKeepingEnabled
                        || variant.StockQuantity - variant.ReservedQuantity > 0,
                    variant.StockKeepingEnabled
                        ? variant.StockQuantity - variant.ReservedQuantity
                        : null,
                    variant.Weight,
                    ParseAttributes(variant.AttributesJson)
                ))
                .ToList()
        );
    }

    private static JsonElement ParseAttributes(string attributesJson)
    {
        using var document = JsonDocument.Parse(attributesJson);
        return document.RootElement.Clone();
    }
}
