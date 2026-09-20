namespace modular_mlm.Application.Organizations.Commands.RemoveOrganizationDomain;

public sealed class RemoveOrganizationDomainCommandValidator
    : AbstractValidator<RemoveOrganizationDomainCommand>
{
    public RemoveOrganizationDomainCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrganizationDomainId).NotEmpty();
    }
}
