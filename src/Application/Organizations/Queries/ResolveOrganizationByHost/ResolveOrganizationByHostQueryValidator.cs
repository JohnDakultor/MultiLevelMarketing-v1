using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost;

public sealed class ResolveOrganizationByHostQueryValidator
    : AbstractValidator<ResolveOrganizationByHostQuery>
{
    public ResolveOrganizationByHostQueryValidator()
    {
        RuleFor(query => query.HostName)
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
