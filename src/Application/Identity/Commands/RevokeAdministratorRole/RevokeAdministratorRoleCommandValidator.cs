namespace modular_mlm.Application.Identity.Commands.RevokeAdministratorRole;

public sealed class RevokeAdministratorRoleCommandValidator
    : AbstractValidator<RevokeAdministratorRoleCommand>
{
    public RevokeAdministratorRoleCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.AdministratorUserId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
