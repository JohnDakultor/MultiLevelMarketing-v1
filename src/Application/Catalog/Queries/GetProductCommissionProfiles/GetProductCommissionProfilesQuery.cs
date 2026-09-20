using modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles.Models;

namespace modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles;

public sealed record GetProductCommissionProfilesQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<ProductCommissionProfileDto>>,
        IOrganizationAdminRequest;
