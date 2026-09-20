using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Queries.GetPayoutAccounts.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutAccounts;

public sealed class GetPayoutAccountsQueryHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db
) : IRequestHandler<GetPayoutAccountsQuery, IReadOnlyList<PayoutAccountDto>>
{
    public async Task<IReadOnlyList<PayoutAccountDto>> Handle(
        GetPayoutAccountsQuery request,
        CancellationToken cancellationToken
    )
    {
        await AuthorizeAsync(request.OrganizationId, request.AgentId, cancellationToken);
        return await db
            .PayoutAccounts.AsNoTracking()
            .Where(account =>
                account.OrganizationId == request.OrganizationId
                && account.AgentId == request.AgentId
            )
            .OrderByDescending(account => account.IsDefault)
            .ThenBy(account => account.Created)
            .Select(account => new PayoutAccountDto(
                account.Id,
                account.Method,
                account.MaskedAccountData,
                account.BankCode,
                account.Rail,
                account.VerificationStatus,
                account.IsDefault
            ))
            .ToListAsync(cancellationToken);
    }

    private async Task AuthorizeAsync(Guid organizationId, Guid agentId, CancellationToken token)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var ownsAgent = await db.Agents.AnyAsync(
            agent =>
                agent.Id == agentId
                && agent.OrganizationId == organizationId
                && agent.UserId == userId,
            token
        );
        if (
            !ownsAgent
            && !await administrators.CanManageOrganizationAsync(userId, organizationId, token)
        )
            throw new ForbiddenAccessException();
    }
}
