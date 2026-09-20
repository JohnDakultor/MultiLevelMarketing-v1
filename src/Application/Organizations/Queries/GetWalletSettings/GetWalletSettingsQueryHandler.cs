using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetWalletSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetWalletSettings;

public sealed class GetWalletSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWalletSettingsQuery, WalletSettingsDto?>
{
    public Task<WalletSettingsDto?> Handle(
        GetWalletSettingsQuery request,
        CancellationToken cancellationToken
    ) =>
        db
            .WalletSettings.AsNoTracking()
            .Where(settings => settings.OrganizationId == request.OrganizationId)
            .Select(settings => new WalletSettingsDto(
                settings.CommissionReleaseTrigger,
                settings.ReleaseDelayDays,
                settings.ReturnWindowDays,
                settings.MinimumPayoutAmount,
                settings.AllowNegativeRecoverableBalance,
                settings.MaximumNegativeBalance
            ))
            .SingleOrDefaultAsync(cancellationToken);
}
