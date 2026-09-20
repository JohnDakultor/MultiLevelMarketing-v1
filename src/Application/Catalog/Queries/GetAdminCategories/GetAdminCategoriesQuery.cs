using modular_mlm.Application.Catalog.Queries.GetAdminCategories.Models;

namespace modular_mlm.Application.Catalog.Queries.GetAdminCategories;

public sealed record GetAdminCategoriesQuery(
    Guid OrganizationId,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 20
) : IRequest<AdminCategoriesPageDto>, IOrganizationAdminRequest;
