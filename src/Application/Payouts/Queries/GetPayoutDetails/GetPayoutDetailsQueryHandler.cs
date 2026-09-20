using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Payouts.Queries.GetPayoutDetails.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutDetails;

public sealed class GetPayoutDetailsQueryHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db
) : IRequestHandler<GetPayoutDetailsQuery, PayoutDetailsDto?>
{
    public async Task<PayoutDetailsDto?> Handle(
        GetPayoutDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var payout = await db
            .PayoutRequests.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.PayoutRequestId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (payout is null)
            return null;
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var ownsAgent = await db.Agents.AnyAsync(
            agent =>
                agent.Id == payout.AgentId
                && agent.OrganizationId == request.OrganizationId
                && agent.UserId == userId,
            cancellationToken
        );
        if (
            !ownsAgent
            && !await administrators.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();
        return new PayoutDetailsDto(
            payout.Id,
            payout.AgentId,
            payout.PayoutAccountId,
            payout.Amount,
            payout.Currency,
            payout.Status,
            payout.RequestedAt,
            payout.ApprovedAt,
            payout.ProcessedAt,
            payout.ProviderBatchId,
            payout.ProviderTransferId,
            payout.ProviderReference,
            payout.FailureCode,
            payout.FailureMessage
        );
    }
}
