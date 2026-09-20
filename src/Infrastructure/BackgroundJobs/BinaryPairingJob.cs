using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Commands.ProcessBinaryPairing;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed record BinaryPairingJobResult(
    Guid OrganizationId,
    Guid CommissionPlanId,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int CandidatesFound,
    int RunsCompleted,
    int RunsSkipped,
    int Failed
);

public sealed class BinaryPairingJob(
    IApplicationDbContext db,
    ISender sender,
    ILogger<BinaryPairingJob> logger
)
{
    public async Task<BinaryPairingJobResult> ExecuteAsync(
        Guid organizationId,
        Guid commissionPlanId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        Validate(organizationId, commissionPlanId, periodStart, periodEnd);
        var plan = await db
            .CommissionPlans.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == commissionPlanId && candidate.OrganizationId == organizationId,
                cancellationToken
            );
        if (plan is null)
            throw new KeyNotFoundException("Commission plan was not found.");
        if (
            plan.Status != CommissionPlanStatus.Active
            || plan.EffectiveFrom >= periodEnd
            || (plan.EffectiveTo.HasValue && plan.EffectiveTo <= periodStart)
        )
            throw new InvalidOperationException("Commission plan is not active for this period.");
        if (!plan.BinaryPairing.Enabled)
            return new(organizationId, commissionPlanId, periodStart, periodEnd, 0, 0, 0, 0);

        var candidateAgentIds = await db
            .BinaryVolumeBalances.AsNoTracking()
            .Where(balance =>
                balance.OrganizationId == organizationId
                && balance.LeftAvailable > 0m
                && balance.RightAvailable > 0m
                && db.Agents.Any(agent =>
                    agent.Id == balance.AgentId
                    && agent.OrganizationId == organizationId
                    && agent.Status == AgentStatus.Active
                )
            )
            .OrderBy(balance => balance.AgentId)
            .Select(balance => balance.AgentId)
            .ToListAsync(cancellationToken);

        var completed = 0;
        var skipped = 0;
        var failed = 0;
        foreach (var agentId in candidateAgentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var key = BuildIdempotencyKey(
                    organizationId,
                    commissionPlanId,
                    agentId,
                    periodStart,
                    periodEnd
                );
                var runId = await sender.Send(
                    new ProcessBinaryPairingCommand(
                        organizationId,
                        agentId,
                        commissionPlanId,
                        periodStart,
                        periodEnd,
                        key
                    ),
                    cancellationToken
                );
                var status = await db
                    .BinaryPairingRuns.AsNoTracking()
                    .Where(run => run.Id == runId)
                    .Select(run => run.Status)
                    .SingleAsync(cancellationToken);
                if (status == BinaryPairingRunStatus.Completed)
                    completed++;
                else
                    skipped++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(
                    exception,
                    "Binary pairing failed for organization {OrganizationId}, plan {CommissionPlanId}, agent {AgentId}, period {PeriodStart} to {PeriodEnd}",
                    organizationId,
                    commissionPlanId,
                    agentId,
                    periodStart,
                    periodEnd
                );
            }
        }

        logger.LogInformation(
            "Binary pairing finished for organization {OrganizationId} and plan {CommissionPlanId}: {CandidatesFound} candidates, {Completed} completed, {Skipped} skipped, {Failed} failed",
            organizationId,
            commissionPlanId,
            candidateAgentIds.Count,
            completed,
            skipped,
            failed
        );
        var tags = new TagList { { "organization.id", organizationId } };
        MarketplaceTelemetry.BinaryPairingDuration.Record(
            Stopwatch.GetElapsedTime(started).TotalMilliseconds,
            tags
        );
        MarketplaceTelemetry.BinaryPairingFailures.Add(failed, tags);
        return new(
            organizationId,
            commissionPlanId,
            periodStart,
            periodEnd,
            candidateAgentIds.Count,
            completed,
            skipped,
            failed
        );
    }

    public static string BuildIdempotencyKey(
        Guid organizationId,
        Guid commissionPlanId,
        Guid agentId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd
    ) =>
        $"binary:{organizationId:N}:{commissionPlanId:N}:{agentId:N}:{periodStart.UtcTicks}:{periodEnd.UtcTicks}";

    private static void Validate(
        Guid organizationId,
        Guid commissionPlanId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd
    )
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Organization is required.", nameof(organizationId));
        if (commissionPlanId == Guid.Empty)
            throw new ArgumentException("Commission plan is required.", nameof(commissionPlanId));
        if (periodStart >= periodEnd)
            throw new ArgumentException("Period start must be before period end.");
    }
}
