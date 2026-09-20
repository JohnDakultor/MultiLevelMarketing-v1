using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.ConfigureOrganizationDomain;

public sealed class ConfigureOrganizationDomainCommandValidator
    : AbstractValidator<ConfigureOrganizationDomainCommand>
{
    public ConfigureOrganizationDomainCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.HostName)
            .NotEmpty()
            .MaximumLength(OrganizationDomain.MaximumHostNameLength)
            .Must(BeValidHostName)
            .WithMessage("A valid public DNS hostname is required.");
    }

    private static bool BeValidHostName(string hostName)
    {
        try
        {
            _ = OrganizationDomain.NormalizeHostName(hostName);
            return true;
        }
        catch (DomainInvariantException)
        {
            return false;
        }
    }
}
