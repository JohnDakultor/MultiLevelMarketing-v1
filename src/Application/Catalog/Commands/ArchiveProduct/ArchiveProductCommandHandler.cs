using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Commands.ArchiveProduct;

public sealed class ArchiveProductCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<ArchiveProductCommand>
{
    public async Task Handle(ArchiveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(
            x => x.Id == request.ProductId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (product is null)
            throw new KeyNotFoundException("Product was not found.");

        var beforeJson = AuditJson.Serialize(new { product.Status });
        product.Archive();
        var audit = AuditCoverageMap.ProductArchived;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            beforeJson,
            AuditJson.Serialize(new { product.Status }),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
