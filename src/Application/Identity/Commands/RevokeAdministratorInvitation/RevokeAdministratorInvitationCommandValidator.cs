namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorInvitation;

public sealed class RevokeAdministratorInvitationCommandValidator
    : AbstractValidator<RevokeAdministratorInvitationCommand>
{
    public RevokeAdministratorInvitationCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.InvitationId).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1_000)
            .Must(value => value.All(character => !char.IsControl(character)));
    }
}
