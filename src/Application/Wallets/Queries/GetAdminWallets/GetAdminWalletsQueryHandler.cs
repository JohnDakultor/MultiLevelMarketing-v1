using modular_mlm.Application.Wallets.Queries.GetAdminWallets.Models;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Queries.GetAdminWallets;

public sealed class GetAdminWalletsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminWalletsQuery, AdminWalletsPageDto>
{
    public async Task<AdminWalletsPageDto> Handle(
        GetAdminWalletsQuery request,
        CancellationToken cancellationToken
    )
    {
        var wallets =
            from wallet in db.AgentWallets.AsNoTracking()
            join agent in db.Agents.AsNoTracking()
                on new { wallet.AgentId, wallet.OrganizationId } equals new
                {
                    AgentId = agent.Id,
                    agent.OrganizationId,
                }
            where wallet.OrganizationId == request.OrganizationId
            select new
            {
                wallet.Id,
                wallet.AgentId,
                agent.AgentCode,
                wallet.Currency,
                wallet.Status,
                Pending = db.WalletEntries
                    .Where(entry =>
                        entry.WalletId == wallet.Id
                        && entry.Type == WalletEntryType.PendingCredit
                        && entry.Amount > 0m
                        && !db.WalletEntries.Any(released =>
                            released.ReleasedFromEntryId == entry.Id
                        )
                        && !db.WalletEntries.Any(reversal =>
                            reversal.ReversalOfEntryId == entry.Id
                        )
                    )
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
                Net = db.WalletEntries
                    .Where(entry =>
                        entry.WalletId == wallet.Id
                        && entry.Type != WalletEntryType.PendingCredit
                    )
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
                Held = db.WalletEntries
                    .Where(entry =>
                        entry.WalletId == wallet.Id
                        && entry.Type == WalletEntryType.Hold
                        && !db.WalletEntries.Any(reversal =>
                            reversal.ReversalOfEntryId == entry.Id
                        )
                    )
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
                Paid = db.WalletEntries
                    .Where(entry =>
                        entry.WalletId == wallet.Id
                        && entry.Type == WalletEntryType.Payout
                    )
                    .Sum(entry => (decimal?)entry.Amount) ?? 0m,
            };

        if (request.Status.HasValue)
            wallets = wallets.Where(wallet => wallet.Status == request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim().ToUpperInvariant();
            wallets = wallets.Where(wallet => wallet.AgentCode.Contains(normalizedSearch));
        }
        if (request.NegativeOnly)
            wallets = wallets.Where(wallet => wallet.Net < 0m);

        var totalCount = await wallets.CountAsync(cancellationToken);
        var rows = await wallets
            .OrderBy(wallet => wallet.AgentCode)
            .ThenBy(wallet => wallet.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(wallet => new AdminWalletSummaryDto(
                wallet.Id,
                wallet.AgentId,
                wallet.AgentCode,
                DisplayName: null,
                wallet.Currency,
                wallet.Status,
                wallet.Pending,
                Available: Math.Max(0m, wallet.Net),
                Held: Math.Abs(wallet.Held),
                PaidLifetime: Math.Abs(wallet.Paid),
                RecoverableNegative: Math.Max(0m, -wallet.Net),
                wallet.Net
            ))
            .ToArray();

        return new AdminWalletsPageDto(
            request.OrganizationId,
            items,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
