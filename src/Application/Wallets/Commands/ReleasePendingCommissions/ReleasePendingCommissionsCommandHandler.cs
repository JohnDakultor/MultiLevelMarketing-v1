using System.Diagnostics;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions;

public sealed class ReleasePendingCommissionsCommandHandler(
    IApplicationDbContext db,
    CommissionAvailabilityPolicy availabilityPolicy,
    ICommissionReleaseLock releaseLock,
    TimeProvider timeProvider
) : IRequestHandler<ReleasePendingCommissionsCommand, ReleasePendingCommissionsResult>
{
    public async Task<ReleasePendingCommissionsResult> Handle(
        ReleasePendingCommissionsCommand request,
        CancellationToken cancellationToken
    )
    {
        var startedAt = Stopwatch.GetTimestamp();
        var now = request.CutoffTime ?? timeProvider.GetUtcNow();
        var organization = await db
            .Organizations.Include(candidate => candidate.Wallet)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );

        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");
        if (organization.Status != OrganizationStatus.Active)
            return EmptyResult(request.OrganizationId, startedAt);

        var candidates = await db
            .CommissionTransactions.Where(commission =>
                commission.OrganizationId == request.OrganizationId
                && commission.Status == CommissionStatus.Pending
                && db.WalletEntries.Any(entry =>
                    entry.SourceId == commission.Id && entry.Type == WalletEntryType.PendingCredit
                )
            )
            .OrderBy(commission => commission.Created)
            .ThenBy(commission => commission.Id)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);

        var released = 0;
        var ineligible = 0;
        var alreadyReleased = 0;

        foreach (var commission in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pendingEntries = await db
                .WalletEntries.Where(entry =>
                    entry.SourceId == commission.Id && entry.Type == WalletEntryType.PendingCredit
                )
                .OrderBy(entry => entry.Created)
                .ThenBy(entry => entry.Id)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (pendingEntries.Count != 1)
                throw new InvalidOperationException(
                    $"Commission {commission.Id} must have exactly one pending wallet credit."
                );

            var pendingEntry = pendingEntries[0];
            await using var releaseLease = await releaseLock.AcquireAsync(
                pendingEntry.Id,
                cancellationToken
            );

            if (
                await db
                    .WalletEntries.AsNoTracking()
                    .AnyAsync(
                        entry => entry.ReleasedFromEntryId == pendingEntry.Id,
                        cancellationToken
                    )
            )
            {
                alreadyReleased++;
                continue;
            }

            Order? order = null;
            if (commission.SourceOrderId is { } orderId)
            {
                order = await db.Orders.SingleOrDefaultAsync(
                    candidate =>
                        candidate.Id == orderId
                        && candidate.OrganizationId == request.OrganizationId,
                    cancellationToken
                );
                if (order is null)
                    throw new InvalidOperationException(
                        $"Source order {orderId} for commission {commission.Id} was not found."
                    );
            }

            var decision = availabilityPolicy.Evaluate(
                new CommissionAvailabilityPolicy.CommissionAvailabilityInput
                {
                    CommissionStatus = commission.Status,
                    CommissionCreatedAt = commission.Created,
                    IsOrderBacked = order is not null,
                    OrderPaymentStatus = order?.PaymentStatus,
                    OrderPaidAt = order?.PaidAt,
                    OrderStatus = order?.Status,
                    OrderDeliveredAt = order?.DeliveredAt,
                    IsReversedOrRefunded =
                        order?.PaymentStatus == PaymentStatus.Refunded
                        || order?.Status == OrderStatus.Refunded,
                    WalletSettings = organization.Wallet,
                    CurrentTime = now,
                }
            );

            if (!decision.IsReleasable)
            {
                ineligible++;
                continue;
            }

            var availableAt = decision.EligibleAt ?? now;
            var availableEntry = pendingEntry.Release(availableAt);
            commission.Release(availableAt);
            db.WalletEntries.Add(availableEntry);
            await db.SaveChangesAsync(cancellationToken);
            released++;
        }

        TimeSpan? oldestPendingAge =
            candidates.Count == 0
                ? null
                : TimeSpan.FromTicks(Math.Max(0L, (now - candidates[0].Created).Ticks));

        return new ReleasePendingCommissionsResult(
            request.OrganizationId,
            candidates.Count,
            released,
            ineligible,
            alreadyReleased,
            0,
            oldestPendingAge,
            Stopwatch.GetElapsedTime(startedAt)
        );
    }

    private static ReleasePendingCommissionsResult EmptyResult(
        Guid organizationId,
        long startedAt
    ) => new(organizationId, 0, 0, 0, 0, 0, null, Stopwatch.GetElapsedTime(startedAt));
}
