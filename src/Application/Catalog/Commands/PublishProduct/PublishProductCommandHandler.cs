using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Commands.PublishProduct;

public sealed class PublishProductCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<PublishProductCommand>
{
    public async Task Handle(PublishProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db
            .Products.Include(candidate => candidate.Variants)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.ProductId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );

        if (product is null)
            throw new KeyNotFoundException("Product was not found.");

        var beforeJson = AuditJson.Serialize(new { product.Status });
        product.Publish();
        var audit = AuditCoverageMap.ProductPublished;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            beforeJson,
            AuditJson.Serialize(new { product.Status }),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
