// PURPOSE: create one tenant-owned storefront category and return its identifier.
// DEFINE sealed record CreateCategoryCommand(Guid OrganizationId, string Name, string Slug)
//     : IRequest<Guid>, IOrganizationAdminRequest.
// OrganizationId is the authorization scope and belongs in every persistence predicate.

namespace modular_mlm.Application.Catalog.Commands.CreateCategory;

public sealed record CreateCategoryCommand(Guid OrganizationId, string Name, string Slug)
    : IRequest<Guid>, IOrganizationAdminRequest;