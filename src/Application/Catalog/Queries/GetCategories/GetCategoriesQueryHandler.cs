using modular_mlm.Application.Catalog.Queries.GetCategories.Models;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Queries.GetCategories;

public sealed class GetCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .Categories.AsNoTracking()
            .Where(category =>
                category.OrganizationId == request.OrganizationId && category.IsActive
            )
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.Id, category.Name, category.Slug))
            .ToListAsync(cancellationToken);
}
