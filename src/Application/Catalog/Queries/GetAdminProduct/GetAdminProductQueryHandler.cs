using modular_mlm.Application.Catalog.Queries.GetAdminProduct.Models;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProduct;

public sealed class GetAdminProductQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminProductQuery, AdminProductDetailsDto>
{
    public async Task<AdminProductDetailsDto> Handle(
        GetAdminProductQuery request,
        CancellationToken cancellationToken
    )
    {
        var product = await db
            .Products.AsNoTracking()
            .Where(candidate =>
                candidate.Id == request.ProductId
                && candidate.OrganizationId == request.OrganizationId
            )
            .Select(candidate => new
            {
                Product = candidate,
                CategoryName = db
                    .Categories.Where(category => category.Id == candidate.CategoryId)
                    .Select(category => category.Name)
                    .Single(),
                CommissionProfileName = candidate.CommissionProfileId == null
                    ? null
                    : db
                        .ProductCommissionProfiles.Where(profile =>
                            profile.Id == candidate.CommissionProfileId
                        )
                        .Select(profile => profile.Name)
                        .SingleOrDefault(),
                Variants = candidate
                    .Variants.OrderBy(variant => variant.Sku)
                    .ThenBy(variant => variant.Id)
                    .Select(variant => new AdminProductVariantDto(
                        variant.Id,
                        variant.Sku,
                        variant.Status,
                        variant.Price,
                        variant.BusinessVolume,
                        variant.Weight,
                        variant.AttributesJson,
                        variant.StockKeepingEnabled,
                        variant.StockQuantity,
                        variant.ReservedQuantity,
                        variant.StockKeepingEnabled
                            ? variant.StockQuantity - variant.ReservedQuantity
                            : null,
                        variant.Version
                    ))
                    .ToList(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            throw new KeyNotFoundException("Product was not found in this organization.");

        return new AdminProductDetailsDto(
            product.Product.Id,
            product.Product.Name,
            product.Product.Slug,
            product.Product.Description,
            product.Product.Status,
            product.Product.CategoryId,
            product.CategoryName,
            product.Product.Brand,
            product.Product.DefaultImageUrl,
            product.Product.CommissionProfileId,
            product.CommissionProfileName,
            product.Product.Created,
            product.Product.LastModified,
            product.Variants
        );
    }
}
