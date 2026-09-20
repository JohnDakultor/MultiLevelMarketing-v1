using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateNetworkSettings;

public sealed class UpdateNetworkSettingsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateNetworkSettingsCommand>
{
    public async Task Handle(
        UpdateNetworkSettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            x => x.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Network);
        organization.UpdateNetworkSettings(
            NetworkSettings.Create(
                request.DefaultPlacementStrategy,
                request.AllowAgentPreferredLeg,
                request.MaxQueryDepth,
                request.AutoPlacementEnabled,
                request.RestrictPlacementChangesAfterActivation
            )
        );
        var audit = AuditCoverageMap.NetworkSettingsUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Network),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(NetworkSettings settings) =>
        AuditJson.Serialize(
            new
            {
                settings.DefaultPlacementStrategy,
                settings.AllowAgentPreferredLeg,
                settings.MaxQueryDepth,
                settings.AutoPlacementEnabled,
                settings.RestrictPlacementChangesAfterActivation,
            }
        );
}
