using System.Diagnostics;
using System.Diagnostics.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed record CommissionReleaseJobResult(
    int OrganizationsFound,
    int OrganizationsProcessed,
    int CommissionsReleased,
    int Ineligible,
    int AlreadyReleased,
    int Failed,
    TimeSpan Duration,
    TimeSpan? MaximumLag
);

public sealed class CommissionReleaseJob(
    IApplicationDbContext db,
    ISender sender,
    IOptions<CommissionReleaseOptions> options,
    CommissionReleaseCursor cursor,
    TimeProvider timeProvider,
    ILogger<CommissionReleaseJob> logger
)
{
    public async Task<CommissionReleaseJobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var activity = MarketplaceTelemetry.ActivitySource.StartActivity(
            "commission-release"
        );
        var configuration = options.Value;
        var activeOrganizations = db
            .Organizations.AsNoTracking()
            .Where(organization => organization.Status == OrganizationStatus.Active)
            .OrderBy(organization => organization.Id)
            .Select(organization => organization.Id);
        var offset = cursor.Read();
        var organizationIds = await activeOrganizations
            .Skip(offset)
            .Take(configuration.OrganizationBatchSize)
            .ToListAsync(cancellationToken);
        var firstPageCount = organizationIds.Count;

        if (offset > 0 && firstPageCount < configuration.OrganizationBatchSize)
        {
            var remaining = configuration.OrganizationBatchSize - firstPageCount;
            var wrappedOrganizationIds = await activeOrganizations
                .Take(remaining)
                .ToListAsync(cancellationToken);
            organizationIds.AddRange(wrappedOrganizationIds);
            cursor.Advance(wrappedOrganizationIds.Count);
        }
        else
        {
            cursor.Advance(
                firstPageCount < configuration.OrganizationBatchSize ? 0 : offset + firstPageCount
            );
        }

        var processed = 0;
        var released = 0;
        var ineligible = 0;
        var alreadyReleased = 0;
        var failed = 0;
        TimeSpan? maximumLag = null;
        var cutoffTime = timeProvider.GetUtcNow();

        foreach (var organizationId in organizationIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await sender.Send(
                    new ReleasePendingCommissionsCommand(
                        organizationId,
                        configuration.CommissionBatchSize,
                        cutoffTime
                    ),
                    cancellationToken
                );

                processed++;
                released += result.ReleasedCount;
                ineligible += result.IneligibleCount;
                alreadyReleased += result.AlreadyReleasedCount;
                failed += result.FailureCount;
                if (
                    result.OldestPendingAge is { } lag
                    && (maximumLag is null || lag > maximumLag.Value)
                )
                    maximumLag = lag;
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
                    "Commission release failed for organization {OrganizationId}",
                    organizationId
                );
            }
        }

        var duration = Stopwatch.GetElapsedTime(startedAt);
        TagList tags = new() { { "job.name", "commission_release" } };
        MarketplaceTelemetry.CommissionReleaseOrganizationsProcessed.Add(processed, tags);
        MarketplaceTelemetry.CommissionsReleased.Add(released, tags);
        MarketplaceTelemetry.CommissionReleaseDuration.Record(duration.TotalMilliseconds, tags);
        if (maximumLag is { } recordedLag)
            MarketplaceTelemetry.CommissionReleaseLag.Record(recordedLag.TotalSeconds, tags);
        if (failed > 0)
            MarketplaceTelemetry.BackgroundJobFailures.Add(failed, tags);
        activity?.SetTag("organizations.processed", processed);
        activity?.SetTag("commissions.released", released);
        activity?.SetTag("failures", failed);
        logger.LogInformation(
            "Commission release completed in {Duration}: {OrganizationsProcessed}/{OrganizationsFound} organizations, {Released} released, {Ineligible} pending or ineligible, {AlreadyReleased} already released, {Failed} failed",
            duration,
            processed,
            organizationIds.Count,
            released,
            ineligible,
            alreadyReleased,
            failed
        );

        return new CommissionReleaseJobResult(
            organizationIds.Count,
            processed,
            released,
            ineligible,
            alreadyReleased,
            failed,
            duration,
            maximumLag
        );
    }
}
