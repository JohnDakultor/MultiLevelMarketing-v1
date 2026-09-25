// PURPOSE: remove a category from new storefront use without deleting historical relationships.
// DEFINE sealed record ArchiveCategoryCommand(Guid OrganizationId, Guid CategoryId)
//     : IRequest, IOrganizationAdminRequest.

namespace modular_mlm.Application.Catalog.Commands.ArchiveCategory;


public sealed record ArchiveCategoryCommand(Guid OrganizationId, Guid CategoryId)
    : IRequest, IOrganizationAdminRequest;