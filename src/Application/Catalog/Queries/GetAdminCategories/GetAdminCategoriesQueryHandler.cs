using modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;

namespace modular_mlm.Application.Catalog.Queries.GetAdminCategories;

public sealed class GetAdminCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminCategoriesQuery, AdminCategoriesPageDto>
{
    public async Task<AdminCategoriesPageDto> Handle(
        GetAdminCategoriesQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .Categories.AsNoTracking()
            .Where(category => category.OrganizationId == request.OrganizationId);
        if (!request.IncludeInactive)
            query = query.Where(category => category.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(category => new AdminCategoryDto(
                category.Id,
                category.Name,
                category.Slug,
                category.IsActive,
                db.Products.Count(product =>
                    product.OrganizationId == request.OrganizationId
                    && product.CategoryId == category.Id
                ),
                category.Created,
                category.LastModified
            ))
            .ToListAsync(cancellationToken);

        return new AdminCategoriesPageDto(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)
        );
    }
}
