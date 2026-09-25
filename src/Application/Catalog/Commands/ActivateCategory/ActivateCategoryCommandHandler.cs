// IMPLEMENT IRequestHandler<ActivateCategoryCommand>; inject IApplicationDbContext and IAuditWriter.
// Load tracked Category by OrganizationId + CategoryId; cross-tenant ids must look not-found.
// Capture IsActive, call Category.Activate(), audit before/after state, and save once.
// Do not mutate linked products; visibility is derived by storefront queries.
// TEST activation, repeat semantics, tenant isolation, audit content, and cancellation.

using modular_mlm.Application.Common.Auditing;

namespace modular_mlm.Application.Catalog.Commands.ActivateCategory;

public sealed class ActivateCategoryCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter) : IRequestHandler<ActivateCategoryCommand>
{
    public async Task Handle(ActivateCategoryCommand request, CancellationToken cancellationToken)
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

        category.Activate();

        var afterJson = AuditJson.Serialize(new { category.IsActive });

        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.CategoryActivated.Action,
            AuditCoverageMap.CategoryActivated.EntityType,
            category.Id,
            beforeJson,
            afterJson,
            reason: null
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
