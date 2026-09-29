using modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts.Models;

namespace modular_mlm.Application.Payouts.Queries.GetAdminPayoutAccounts;

public sealed class GetAdminPayoutAccountsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminPayoutAccountsQuery, AdminPayoutAccountsPageDto>
{
    public async Task<AdminPayoutAccountsPageDto> Handle(
        GetAdminPayoutAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        var accounts =
            from account in db.PayoutAccounts.AsNoTracking()
            join agent in db.Agents.AsNoTracking()
                on new { account.AgentId, account.OrganizationId } equals new
                {
                    AgentId = agent.Id,
                    agent.OrganizationId,
                }
            where account.OrganizationId == request.OrganizationId
            select new { Account = account, agent.AgentCode };

        if (request.Status.HasValue)
            accounts = accounts.Where(row =>
                row.Account.VerificationStatus == request.Status.Value
            );

        var totalCount = await accounts.CountAsync(cancellationToken);
        var items = await accounts
            .OrderBy(row => row.Account.VerificationStatus)
            .ThenByDescending(row => row.Account.Created)
            .ThenBy(row => row.Account.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new AdminPayoutAccountDto(
                row.Account.Id,
                row.Account.AgentId,
                row.AgentCode,
                row.Account.Method,
                row.Account.MaskedAccountData,
                row.Account.BankCode,
                row.Account.Rail,
                row.Account.VerificationStatus,
                row.Account.IsDefault,
                row.Account.Created
            ))
            .ToListAsync(cancellationToken);

        return new AdminPayoutAccountsPageDto(
            request.OrganizationId,
            items,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
