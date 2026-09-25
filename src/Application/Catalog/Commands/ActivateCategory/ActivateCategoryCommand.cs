// PURPOSE: restore an archived category to active storefront eligibility.
// DEFINE sealed record ActivateCategoryCommand(Guid OrganizationId, Guid CategoryId)
//     : IRequest, IOrganizationAdminRequest.
namespace modular_mlm.Application.Catalog.Commands.ActivateCategory;


public sealed record ActivateCategoryCommand(
    Guid OrganizationId,
    Guid CategoryId
) : IRequest, IOrganizationAdminRequest;