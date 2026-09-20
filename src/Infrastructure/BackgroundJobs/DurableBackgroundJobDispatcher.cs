using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class DurableBackgroundJobDispatcher(
    ApplicationDbContext db,
    IEnumerable<IBackgroundJobHandler> handlers,
    IOptions<DurableBackgroundJobOptions> options,
    OutboxDeadLetterAlertSender deadLetterAlerts,
    TimeProvider clock,
    ILogger<DurableBackgroundJobDispatcher> logger
)
{
    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        var now = clock.GetUtcNow();
        var workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
        var candidateIds = await db
            .DurableBackgroundJobs.AsNoTracking()
            .Where(job =>
                job.CompletedAt == null
                && job.DeadLetteredAt == null
                && job.NextAttemptAt <= now
                && (job.ClaimedUntil == null || job.ClaimedUntil < now)
            )
            .OrderBy(job => job.NextAttemptAt)
            .ThenBy(job => job.Id)
            .Select(job => job.Id)
            .Take(configuration.BatchSize)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0)
            return 0;

        await db
            .DurableBackgroundJobs.Where(job =>
                candidateIds.Contains(job.Id)
                && job.CompletedAt == null
                && job.DeadLetteredAt == null
                && (job.ClaimedUntil == null || job.ClaimedUntil < now)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(job => job.ClaimedBy, workerId)
                        .SetProperty(
                            job => job.ClaimedUntil,
                            now.AddSeconds(configuration.ClaimSeconds)
                        ),
                cancellationToken
            );
        var claimed = await db
            .DurableBackgroundJobs.Where(job => job.ClaimedBy == workerId)
            .OrderBy(job => job.NextAttemptAt)
            .ToListAsync(cancellationToken);
        var registry = handlers.ToDictionary(handler => handler.JobName, StringComparer.Ordinal);

        foreach (var job in claimed)
        {
            try
            {
                if (!registry.TryGetValue(job.JobName, out var handler))
                    throw new InvalidOperationException(
                        $"No handler is registered for durable job '{job.JobName}'."
                    );
                await handler.HandleAsync(
                    new DurableJobPayload(
                        job.Id,
                        job.OrganizationId,
                        job.JobName,
                        job.PayloadJson,
                        job.IdempotencyKey,
                        job.CorrelationId,
                        job.CreatedAt,
                        job.NextAttemptAt
                    ),
                    cancellationToken
                );
                job.MarkCompleted(clock.GetUtcNow());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var attempt = job.Attempts + 1;
                var deadLettered = job.MarkFailed(
                    exception.GetType().Name,
                    clock.GetUtcNow(),
                    configuration.MaximumAttempts,
                    RetryDelay(configuration, attempt)
                );
                logger.LogError(
                    exception,
                    "Durable job {JobId} ({JobName}) failed on attempt {Attempt}",
                    job.Id,
                    job.JobName,
                    attempt
                );
                if (
                    deadLettered
                    && job.OrganizationId is { } organizationId
                    && job.Attempts >= configuration.DeadLetterAlertThreshold
                )
                    await deadLetterAlerts.SendAsync(
                        organizationId,
                        job.Id,
                        $"durable-job:{job.JobName}",
                        cancellationToken
                    );
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        return claimed.Count;
    }

    private static TimeSpan RetryDelay(DurableBackgroundJobOptions options, int attempt)
    {
        var seconds = Math.Min(
            options.MaximumRetrySeconds,
            options.InitialRetrySeconds * Math.Pow(2, Math.Max(0, attempt - 1))
        );
        return TimeSpan.FromSeconds(seconds);
    }
}
