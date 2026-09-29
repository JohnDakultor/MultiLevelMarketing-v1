using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Common.Features;

public sealed class OrganizationFeatureGate(IApplicationDbContext db) : IOrganizationFeatureGate
{
    public async Task EnsureEnabledAsync(
        Guid organizationId,
        OrganizationFeature feature,
        CancellationToken cancellationToken
    )
    {
        if (organizationId == Guid.Empty)
            return;

        var organization = await db
            .Organizations.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == organizationId, cancellationToken);
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var enabled = feature switch
        {
            OrganizationFeature.Commerce => organization.Features.CommerceEnabled,
            OrganizationFeature.AgentProgram => organization.Features.AgentProgramEnabled,
            OrganizationFeature.BinaryNetwork => organization.Features.BinaryNetworkEnabled,
            OrganizationFeature.BinaryPairing => organization.Features.BinaryPairingEnabled,
            OrganizationFeature.Wallet => organization.Features.WalletEnabled,
            OrganizationFeature.Payout => organization.Features.PayoutEnabled,
            _ => false,
        };
        if (!enabled)
            throw new ConflictException(
                $"The {GetDisplayName(feature)} feature is disabled for this organization."
            );
    }

    private static string GetDisplayName(OrganizationFeature feature) =>
        feature switch
        {
            OrganizationFeature.AgentProgram => "agent program",
            OrganizationFeature.BinaryNetwork => "binary network",
            OrganizationFeature.BinaryPairing => "binary pairing",
            _ => feature.ToString().ToLowerInvariant(),
        };
}
