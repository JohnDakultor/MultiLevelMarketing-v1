// IMPLEMENT IRequestHandler<ArchiveCategoryCommand>; inject IApplicationDbContext and IAuditWriter.
// Load tracked tenant Category; missing/cross-tenant ids return not-found.
// Document the active-product policy: reject archival, or hide category while products remain available.
// CALL Category.Archive(); retain ProductCategory links and historical references.
// Audit old/new IsActive and save once; ensure storefront reads exclude inactive categories.
// TEST the selected product policy, repeat semantics, tenant isolation, audit, and cancellation.

using modular_mlm.Application.Common.Auditing;

namespace modular_mlm.Application.Catalog.Commands.ArchiveCategory;

public sealed class ArchiveCategoryCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter) : IRequestHandler<ArchiveCategoryCommand>
{
    public async Task Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(
            category =>
                category.OrganizationId == request.OrganizationId
                && category.Id == request.CategoryId,
            cancellationToken
        );

        if (category is null)
            throw new KeyNotFoundException("Category was not found.");


        var beforeJson = AuditJson.Serialize(new { category.IsActive });

        category.Archive();

        var afterJson = AuditJson.Serialize(new { category.IsActive });

        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.CategoryArchived.Action,
            AuditCoverageMap.CategoryArchived.EntityType,
            category.Id,
            beforeJson,
            afterJson,
            reason: null
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
