using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Payouts.Models;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Payouts;

namespace modular_mlm.Application.Payouts.EventHandlers;

public sealed class PayoutApprovedEventHandler(
    IApplicationDbContext db,
    IBackgroundJobScheduler backgroundJobs
) : INotificationHandler<PayoutApprovedEvent>
{
    public async Task Handle(PayoutApprovedEvent notification, CancellationToken cancellationToken)
    {
        var payout = await db
            .PayoutRequests.AsNoTracking()
            .Where(candidate =>
                candidate.Id == notification.PayoutRequestId
                && candidate.OrganizationId == notification.OrganizationId
            )
            .Select(candidate => new
            {
                candidate.AgentId,
                candidate.Amount,
                candidate.Currency,
                candidate.Status,
                candidate.ApprovedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payout is null)
            throw new KeyNotFoundException(
                "The payout request referenced by the approval event was not found."
            );
        if (payout.AgentId != notification.AgentId)
            throw new InvalidOperationException(
                "The payout approval event does not match the persisted payout request."
            );

        if (
            payout.Amount != notification.Amount
            || !string.Equals(
                payout.Currency,
                notification.Currency,
                StringComparison.OrdinalIgnoreCase
            )
            || payout.ApprovedAt is null
        )
            throw new InvalidOperationException(
                "The payout approval event does not match the persisted payout request."
            );

        if (payout.Status is PayoutStatus.Processing or PayoutStatus.Paid or PayoutStatus.Failed)
            return;
        if (payout.Status != PayoutStatus.Approved)
            throw new InvalidOperationException(
                $"A payout in status '{payout.Status}' cannot be scheduled for processing."
            );

        await backgroundJobs.EnqueueAsync(
            DurableJobRegistry.ProcessCommissionPayout,
            new ProcessPayoutJobPayload(
                notification.OrganizationId,
                notification.PayoutRequestId,
                $"process-payout:{notification.PayoutRequestId:N}"
            ),
            cancellationToken
        );
    }
}
