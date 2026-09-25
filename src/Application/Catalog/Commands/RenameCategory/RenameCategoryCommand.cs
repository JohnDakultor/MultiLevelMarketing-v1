// PURPOSE: rename an existing tenant category without changing slug, tenant, or lifecycle state.
// DEFINE sealed record RenameCategoryCommand(Guid OrganizationId, Guid CategoryId, string Name)
//     : IRequest, IOrganizationAdminRequest.

namespace modular_mlm.Application.Catalog.Commands.RenameCategory;

public sealed record RenameCategoryCommand (
    Guid OrganizationId,
    Guid CategoryId,
    string Name
) : IRequest, IOrganizationAdminRequest;