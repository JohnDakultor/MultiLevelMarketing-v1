using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed record BinaryPairingPeriod(DateTimeOffset Start, DateTimeOffset End);

public sealed class BinaryPairingScheduleProcessor(
    IApplicationDbContext db,
    BinaryPairingJob job,
    TimeProvider clock,
    ILogger<BinaryPairingScheduleProcessor> logger
)
{
    public async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var schedules = await (
            from plan in db.CommissionPlans.AsNoTracking()
            join organization in db.Organizations.AsNoTracking()
                on plan.OrganizationId equals organization.Id
            where
                plan.Status == CommissionPlanStatus.Active
                && plan.BinaryPairing.Enabled
                && organization.Status == OrganizationStatus.Active
            select new PairingSchedule(
                organization.Id,
                organization.TimeZone,
                plan.Id,
                plan.EffectiveFrom,
                plan.EffectiveTo,
                plan.BinaryPairing.ProcessingFrequency
            )
        ).ToListAsync(cancellationToken);

        foreach (var schedule in schedules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
                var period = CalculateMostRecentCompletedPeriod(now, timeZone, schedule.Frequency);

                if (
                    schedule.EffectiveFrom >= period.End
                    || (schedule.EffectiveTo.HasValue && schedule.EffectiveTo <= period.Start)
                )
                    continue;

                var candidateAgentIds = await GetCandidateAgentIdsAsync(
                    schedule.OrganizationId,
                    cancellationToken
                );
                if (candidateAgentIds.Count == 0)
                    continue;

                var processedAgentIds = await db
                    .BinaryPairingRuns.AsNoTracking()
                    .Where(run =>
                        run.OrganizationId == schedule.OrganizationId
                        && run.CommissionPlanId == schedule.CommissionPlanId
                        && run.PeriodStart == period.Start
                        && run.PeriodEnd == period.End
                        && candidateAgentIds.Contains(run.AgentId)
                    )
                    .Select(run => run.AgentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (processedAgentIds.Count == candidateAgentIds.Count)
                    continue;

                var result = await job.ExecuteAsync(
                    schedule.OrganizationId,
                    schedule.CommissionPlanId,
                    period.Start,
                    period.End,
                    cancellationToken
                );

                logger.LogInformation(
                    "Scheduled binary pairing processed organization {OrganizationId}, plan {CommissionPlanId}, period {PeriodStart} to {PeriodEnd}: {Completed} completed, {Skipped} skipped, {Failed} failed",
                    schedule.OrganizationId,
                    schedule.CommissionPlanId,
                    period.Start,
                    period.End,
                    result.RunsCompleted,
                    result.RunsSkipped,
                    result.Failed
                );
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Unable to process the binary pairing schedule for organization {OrganizationId} and plan {CommissionPlanId}",
                    schedule.OrganizationId,
                    schedule.CommissionPlanId
                );
            }
        }
    }

    public static BinaryPairingPeriod CalculateMostRecentCompletedPeriod(
        DateTimeOffset utcNow,
        TimeZoneInfo timeZone,
        ProcessingFrequency frequency
    )
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, timeZone).DateTime);
        DateOnly periodStart;
        DateOnly periodEnd;

        switch (frequency)
        {
            case ProcessingFrequency.Daily:
                periodEnd = localDate;
                periodStart = periodEnd.AddDays(-1);
                break;
            case ProcessingFrequency.Weekly:
                var daysSinceMonday = ((int)localDate.DayOfWeek + 6) % 7;
                periodEnd = localDate.AddDays(-daysSinceMonday);
                periodStart = periodEnd.AddDays(-7);
                break;
            case ProcessingFrequency.Monthly:
                periodEnd = new DateOnly(localDate.Year, localDate.Month, 1);
                periodStart = periodEnd.AddMonths(-1);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(frequency),
                    frequency,
                    "Unsupported pairing processing frequency."
                );
        }

        return new BinaryPairingPeriod(
            ConvertLocalBoundaryToUtc(periodStart, timeZone),
            ConvertLocalBoundaryToUtc(periodEnd, timeZone)
        );
    }

    private async Task<List<Guid>> GetCandidateAgentIdsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    ) =>
        await db
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

    private static DateTimeOffset ConvertLocalBoundaryToUtc(
        DateOnly localDate,
        TimeZoneInfo timeZone
    )
    {
        var localBoundary = DateTime.SpecifyKind(
            localDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified
        );
        var utcBoundary = TimeZoneInfo.ConvertTimeToUtc(localBoundary, timeZone);
        return new DateTimeOffset(utcBoundary, TimeSpan.Zero);
    }

    private sealed record PairingSchedule(
        Guid OrganizationId,
        string TimeZoneId,
        Guid CommissionPlanId,
        DateTimeOffset EffectiveFrom,
        DateTimeOffset? EffectiveTo,
        ProcessingFrequency Frequency
    );
}
