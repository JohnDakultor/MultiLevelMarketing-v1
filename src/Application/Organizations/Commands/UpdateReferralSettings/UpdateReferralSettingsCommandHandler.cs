using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateReferralSettings;

public sealed class UpdateReferralSettingsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateReferralSettingsCommand>
{
    public async Task Handle(
        UpdateReferralSettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            x => x.Id == request.OrganizationId,
            cancellationToken
        );

        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Referrals);
        organization.UpdateReferralSettings(
            ReferralSettings.Create(
                request.AttributionWindowDays,
                request.AllowReferralOverride,
                request.ReferralLockAfterFirstPurchase
            )
        );
        var audit = AuditCoverageMap.ReferralSettingsUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Referrals),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(ReferralSettings settings) =>
        AuditJson.Serialize(
            new
            {
                settings.AttributionWindowDays,
                settings.AllowReferralOverride,
                settings.ReferralLockAfterFirstPurchase,
            }
        );
}
