using modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost.Models;

namespace modular_mlm.Web.Infrastructure.Tenancy;

public sealed class ResolvedOrganizationContext
{
    private ResolvedOrganizationDto? _organization;

    public ResolvedOrganizationDto? Organization => _organization;
    public Guid? OrganizationId => _organization?.OrganizationId;
    public string? Slug => _organization?.Slug;
    public string? HostName => _organization?.HostName;

    public void Initialize(ResolvedOrganizationDto organization)
    {
        ArgumentNullException.ThrowIfNull(organization);
        if (_organization is not null)
            throw new InvalidOperationException("Organization context is already initialized.");
        _organization = organization;
    }
}
