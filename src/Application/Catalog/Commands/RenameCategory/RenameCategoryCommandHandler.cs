// IMPLEMENT IRequestHandler<RenameCategoryCommand>; inject IApplicationDbContext and IAuditWriter.
// Load tracked Category by OrganizationId AND CategoryId; cross-tenant ids must return not-found.
// Trim Name, capture the old value, and call Category.Rename so domain rules remain authoritative.
// Write before/after audit JSON, then SaveChangesAsync once.
// TEST not-found, tenant isolation, rename, repeated value behavior, audit, and cancellation.

using modular_mlm.Application.Common.Auditing;

namespace modular_mlm.Application.Catalog.Commands.RenameCategory;


public sealed class RenameCategoryCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter) : IRequestHandler<RenameCategoryCommand>
{
    public async Task Handle(RenameCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        var category = await db.Categories.SingleOrDefaultAsync(
            category => category.OrganizationId == request.OrganizationId && category.Id == request.CategoryId,
            cancellationToken
        );

        if (category is null)
            throw new KeyNotFoundException("Category was not found.");

        var beforeJson = AuditJson.Serialize(new { category.Name });

        category.Rename(name);

        var afterJson = AuditJson.Serialize(new { category.Name });


        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.CategoryRenamed.Action,
            AuditCoverageMap.CategoryRenamed.EntityType,
            category.Id,
            beforeJson,
            afterJson,
            reason: null
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
