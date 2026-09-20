using Domain.Services;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Wallets.Queries.GetWalletSummary.Models;

namespace modular_mlm.Application.Wallets.Queries.GetWalletSummary;

public sealed class GetWalletSummaryQueryHandler(
    IApplicationDbContext db,
    WalletBalanceCalculator balanceCalculator
) : IRequestHandler<GetWalletSummaryQuery, WalletSummaryDto?>
{
    public async Task<WalletSummaryDto?> Handle(
        GetWalletSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var wallet = await db
            .AgentWallets.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == request.OrganizationId && x.AgentId == request.AgentId,
                cancellationToken
            );
        if (wallet is null)
            return null;
        var entries = await db
            .WalletEntries.AsNoTracking()
            .Where(entry => entry.WalletId == wallet.Id)
            .ToListAsync(cancellationToken);
        var balance = balanceCalculator.Calculate(entries);

        return new WalletSummaryDto(
            wallet.Id,
            wallet.Currency,
            balance.Pending,
            balance.Available,
            balance.Held,
            balance.PaidOut,
            balance.RecoverableNegative,
            balance.Net
        );
    }
}
