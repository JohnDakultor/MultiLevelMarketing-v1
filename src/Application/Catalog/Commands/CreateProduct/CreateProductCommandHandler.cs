using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken
    )
    {
        var categoryExists = await db.Categories.AnyAsync(
            category =>
                category.Id == request.CategoryId
                && category.OrganizationId == request.OrganizationId
                && category.IsActive,
            cancellationToken
        );

        if (!categoryExists)
            throw new KeyNotFoundException("Active category was not found in this organization.");

        var product = Product.Create(
            request.OrganizationId,
            request.CategoryId,
            request.Name,
            request.Slug,
            request.Description
        );
        product.AddVariant(
            request.Sku,
            request.Price,
            request.BusinessVolume,
            request.StockQuantity
        );
        db.Products.Add(product);
        var audit = AuditCoverageMap.ProductCreated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            beforeJson: null,
            AuditJson.Serialize(
                new
                {
                    product.Name,
                    product.Slug,
                    product.Description,
                    product.CategoryId,
                    product.Status,
                    VariantCount = product.Variants.Count,
                }
            ),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
