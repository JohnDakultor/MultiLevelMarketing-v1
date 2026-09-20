namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorInvitation;

public sealed record RevokeAdministratorInvitationCommand(
    Guid OrganizationId,
    Guid InvitationId,
    string Reason
) : IRequest, IOrganizationAdminRequest;
