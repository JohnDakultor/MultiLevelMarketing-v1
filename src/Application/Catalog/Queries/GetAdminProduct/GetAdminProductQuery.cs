using modular_mlm.Application.Catalog.Queries.GetAdminProduct.Models;

namespace modular_mlm.Application.Catalog.Queries.GetAdminProduct;

public sealed record GetAdminProductQuery(Guid OrganizationId, Guid ProductId)
    : IRequest<AdminProductDetailsDto>,
        IOrganizationAdminRequest;
