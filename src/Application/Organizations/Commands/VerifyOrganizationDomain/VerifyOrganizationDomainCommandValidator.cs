namespace modular_mlm.Application.Organizations.Commands.VerifyOrganizationDomain;

public sealed class VerifyOrganizationDomainCommandValidator
    : AbstractValidator<VerifyOrganizationDomainCommand>
{
    public VerifyOrganizationDomainCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.OrganizationDomainId).NotEmpty();
    }
}
