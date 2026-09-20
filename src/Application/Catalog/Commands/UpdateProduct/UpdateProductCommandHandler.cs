using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(
            x => x.Id == request.ProductId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (product is null)
            throw new KeyNotFoundException("Product was not found.");
        if (
            !await db.Categories.AnyAsync(
                x =>
                    x.Id == request.CategoryId
                    && x.OrganizationId == request.OrganizationId
                    && x.IsActive,
                cancellationToken
            )
        )
            throw new KeyNotFoundException("Active category was not found.");

        var beforeJson = AuditJson.Serialize(
            new
            {
                product.Name,
                product.Description,
                product.CategoryId,
            }
        );
        product.UpdateDetails(request.Name, request.Description, request.CategoryId);
        var audit = AuditCoverageMap.ProductUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            beforeJson,
            AuditJson.Serialize(
                new
                {
                    product.Name,
                    product.Description,
                    product.CategoryId,
                }
            ),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
