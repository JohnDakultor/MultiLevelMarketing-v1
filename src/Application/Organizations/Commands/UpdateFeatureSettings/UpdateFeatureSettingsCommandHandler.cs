using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateFeatureSettings;

public sealed class UpdateFeatureSettingsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateFeatureSettingsCommand>
{
    public async Task Handle(
        UpdateFeatureSettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            x => x.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Features);
        var commerceWasEnabled = organization.Features.CommerceEnabled;
        var features = FeatureSettings.Default();
        features.SetCommerce(request.CommerceEnabled);
        features.SetAgentProgram(request.AgentProgramEnabled);
        features.SetBinaryNetwork(request.BinaryNetworkEnabled);
        features.SetBinaryPairing(request.BinaryPairingEnabled);
        features.SetWallet(request.WalletEnabled);
        features.SetPayout(request.PayoutEnabled);
        organization.UpdateFeatures(features);
        var audit = AuditCoverageMap.FeaturesUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Features),
            null
        );
        if (commerceWasEnabled != organization.Features.CommerceEnabled)
        {
            var commerceAudit = AuditCoverageMap.CommerceSettingsUpdated;
            auditWriter.Write(
                request.OrganizationId,
                commerceAudit.Action,
                commerceAudit.EntityType,
                organization.Id,
                AuditJson.Serialize(new { CommerceEnabled = commerceWasEnabled }),
                AuditJson.Serialize(
                    new { CommerceEnabled = organization.Features.CommerceEnabled }
                ),
                null
            );
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(FeatureSettings value) =>
        AuditJson.Serialize(
            new
            {
                value.CommerceEnabled,
                value.AgentProgramEnabled,
                value.BinaryNetworkEnabled,
                value.BinaryPairingEnabled,
                value.WalletEnabled,
                value.PayoutEnabled,
                value.ReviewsEnabled,
                value.CouponsEnabled,
            }
        );
}
