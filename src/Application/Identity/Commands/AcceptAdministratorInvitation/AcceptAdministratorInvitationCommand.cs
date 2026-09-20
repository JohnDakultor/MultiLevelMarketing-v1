namespace modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;

public record AcceptAdministratorInvitationCommand(
    string RawToken,
    string DisplayName,
    string Password,
    string ConfirmPassword
) : IRequest<Guid>;
