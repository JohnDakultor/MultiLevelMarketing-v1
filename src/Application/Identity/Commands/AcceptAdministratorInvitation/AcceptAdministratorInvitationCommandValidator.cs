namespace modular_mlm.Application.Identity.Commands.AcceptAdministratorInvitation;

public class AcceptAdministratorInvitationCommandValidator
    : AbstractValidator<AcceptAdministratorInvitationCommand>
{
    public AcceptAdministratorInvitationCommandValidator()
    {
        RuleFor(v => v.RawToken).NotEmpty().MaximumLength(256);

        RuleFor(v => v.DisplayName).NotEmpty().MaximumLength(100);

        RuleFor(v => v.Password).NotEmpty().MaximumLength(256);

        RuleFor(v => v.ConfirmPassword).NotEmpty().MaximumLength(256).Equal(v => v.Password);
    }
}
