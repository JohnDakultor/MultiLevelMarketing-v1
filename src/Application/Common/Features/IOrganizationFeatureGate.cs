namespace modular_mlm.Application.Common.Features;

public interface IOrganizationFeatureGate
{
    Task EnsureEnabledAsync(
        Guid organizationId,
        OrganizationFeature feature,
        CancellationToken cancellationToken
    );
}
