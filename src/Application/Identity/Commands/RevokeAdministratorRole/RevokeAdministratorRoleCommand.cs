namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorRole;

public sealed record RevokeAdministratorRoleCommand(
    Guid OrganizationId,
    Guid AdministratorUserId,
    string Reason
) : IRequest, IOrganizationAdminRequest;
