using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Payouts;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Monitoring;

public sealed class OperationalHealthReader(ApplicationDbContext db) : IOperationalHealthReader
{
    public async Task<OperationalHealthSnapshot> ReadAsync(
        Guid organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var pendingOutbox = db
            .OutboxMessages.AsNoTracking()
            .Where(message =>
                message.OrganizationId == organizationId
                && message.ProcessedAt == null
                && message.DeadLetteredAt == null
            );
        var oldest = await pendingOutbox
            .OrderBy(message => message.OccurredAt)
            .Select(message => (DateTimeOffset?)message.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
        var latestPairing = await db
            .BinaryPairingRuns.AsNoTracking()
            .Where(run => run.OrganizationId == organizationId && run.ProcessedAt != null)
            .OrderByDescending(run => run.ProcessedAt)
            .Select(run => new { run.PeriodEnd, run.ProcessedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return new OperationalHealthSnapshot(
            await pendingOutbox.CountAsync(cancellationToken),
            oldest.HasValue ? Math.Max(0, (now - oldest.Value).TotalSeconds) : null,
            await db.WebhookProcessingAttempts.CountAsync(
                attempt =>
                    attempt.OrganizationId == organizationId
                    && (attempt.Status == "Failed" || attempt.Status == "DeadLettered"),
                cancellationToken
            ),
            await db.Payments.CountAsync(
                payment =>
                    payment.OrganizationId == organizationId
                    && payment.ProviderPaymentId != null
                    && payment.Status == PaymentStatus.PartiallyRefunded,
                cancellationToken
            ),
            await db.PayoutRequests.CountAsync(
                payout =>
                    payout.OrganizationId == organizationId
                    && payout.Status == PayoutStatus.Processing,
                cancellationToken
            ),
            await db.Payments.CountAsync(
                payment =>
                    payment.OrganizationId == organizationId
                    && payment.Status == PaymentStatus.Paid
                    && payment.CompensationProcessedAt == null,
                cancellationToken
            ),
            await db.BinaryPairingRuns.CountAsync(
                run =>
                    run.OrganizationId == organizationId
                    && run.Status == BinaryPairingRunStatus.Failed
                    && run.PeriodEnd >= now.AddDays(-1),
                cancellationToken
            ),
            latestPairing?.ProcessedAt is not null
                ? Math.Max(
                    0,
                    (latestPairing.ProcessedAt.Value - latestPairing.PeriodEnd).TotalMilliseconds
                )
                : null
        );
    }
}
