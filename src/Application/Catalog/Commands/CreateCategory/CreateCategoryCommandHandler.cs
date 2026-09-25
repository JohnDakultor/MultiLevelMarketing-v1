using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Persistence;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.CreateCategory;

public sealed class CreateCategoryCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    IDatabaseExceptionClassifier databaseExceptionClassifier
) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var normalizedName = request.Name.Trim();

        var slugExists = await db
            .Categories.AsNoTracking()
            .AnyAsync(
                category =>
                    category.OrganizationId == request.OrganizationId
                    && category.Slug == normalizedSlug,
                cancellationToken
            );

        if (slugExists)
            throw new ConflictException("The category slug is already in use.");

        var category = Category.Create(request.OrganizationId, normalizedName, normalizedSlug);

        db.Categories.Add(category);

        var audit = AuditCoverageMap.CategoryCreated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            category.Id,
            beforeJson: null,
            AuditJson.Serialize(
                new
                {
                    category.Name,
                    category.Slug,
                    category.IsActive,
                }
            ),
            reason: null
        );

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (databaseExceptionClassifier.IsUniqueConstraintViolation(
                exception,
                DatabaseConstraintNames.CategoryOrganizationSlug
            ))
        {
            throw new ConflictException("The category slug is already in use.");
        }

        return category.Id;
    }
}
