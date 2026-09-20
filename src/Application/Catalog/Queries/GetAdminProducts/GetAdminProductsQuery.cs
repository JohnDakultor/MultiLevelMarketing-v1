using modular_mlm.Application.Catalog.Queries.GetAdminProducts.Models;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProducts;

public sealed record GetAdminProductsQuery(Guid OrganizationId, int Page = 1, int PageSize = 100)
    : IRequest<IReadOnlyList<AdminProductDto>>,
        IOrganizationAdminRequest;
