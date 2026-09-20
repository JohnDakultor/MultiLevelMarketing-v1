using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateWalletSettings;

public sealed class UpdateWalletSettingsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateWalletSettingsCommand>
{
    public async Task Handle(
        UpdateWalletSettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db
            .Organizations.Include(candidate => candidate.Wallet)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );

        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Wallet);
        organization.Wallet.Update(
            request.CommissionReleaseTrigger,
            request.ReleaseDelayDays,
            request.ReturnWindowDays,
            request.MinimumPayoutAmount,
            request.AllowNegativeRecoverableBalance,
            request.MaximumNegativeBalance
        );

        var audit = AuditCoverageMap.WalletSettingsUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Wallet),
            reason: null
        );

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(WalletSettings settings) =>
        AuditJson.Serialize(
            new
            {
                settings.CommissionReleaseTrigger,
                settings.ReleaseDelayDays,
                settings.ReturnWindowDays,
                settings.MinimumPayoutAmount,
                settings.AllowNegativeRecoverableBalance,
                settings.MaximumNegativeBalance,
            }
        );
}
