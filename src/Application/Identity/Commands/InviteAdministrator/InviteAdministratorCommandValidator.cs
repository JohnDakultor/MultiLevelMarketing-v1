namespace modular_mlm.Application.Identity.Commands.InviteAdministrator;

public class InviteAdministratorCommandValidator : AbstractValidator<InviteAdministratorCommand>
{
    public InviteAdministratorCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
