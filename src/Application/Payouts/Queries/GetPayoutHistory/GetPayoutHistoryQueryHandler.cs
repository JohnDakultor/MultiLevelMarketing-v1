using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Queries.GetPayoutHistory.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutHistory;

public sealed class GetPayoutHistoryQueryHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db
) : IRequestHandler<GetPayoutHistoryQuery, IReadOnlyList<PayoutHistoryItemDto>>
{
    public async Task<IReadOnlyList<PayoutHistoryItemDto>> Handle(
        GetPayoutHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        await AuthorizeAsync(request, cancellationToken);
        var query = db
            .PayoutRequests.AsNoTracking()
            .Where(payout => payout.OrganizationId == request.OrganizationId);
        if (request.AgentId.HasValue)
            query = query.Where(payout => payout.AgentId == request.AgentId.Value);
        return await query
            .OrderByDescending(payout => payout.RequestedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(payout => new PayoutHistoryItemDto(
                payout.Id,
                payout.AgentId,
                payout.Amount,
                payout.Currency,
                payout.Status,
                payout.RequestedAt,
                payout.ProcessedAt,
                payout.ProviderReference
            ))
            .ToListAsync(cancellationToken);
    }

    private async Task AuthorizeAsync(GetPayoutHistoryQuery request, CancellationToken token)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (request.AgentId.HasValue)
        {
            var ownsAgent = await db.Agents.AnyAsync(
                agent =>
                    agent.Id == request.AgentId.Value
                    && agent.OrganizationId == request.OrganizationId
                    && agent.UserId == userId,
                token
            );
            if (ownsAgent)
                return;
        }
        if (!await administrators.CanManageOrganizationAsync(userId, request.OrganizationId, token))
            throw new ForbiddenAccessException();
    }
}
