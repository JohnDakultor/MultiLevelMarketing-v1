using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Domain.Idempotency;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Idempotency;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed record IdempotencyCleanupResult(
    int DeletedCount,
    bool LockAcquired,
    TimeSpan Duration
);

public sealed class IdempotencyCleanupJob(
    ApplicationDbContext db,
    IOptions<IdempotencyOptions> options,
    TimeProvider clock,
    ILogger<IdempotencyCleanupJob> logger
)
{
    private const long AdvisoryLockKey = 6_988_894_169_465_010_025;

    public Task<IdempotencyCleanupResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => ExecuteWithinTransactionAsync(cancellationToken));
    }

    private async Task<IdempotencyCleanupResult> ExecuteWithinTransactionAsync(
        CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        var now = clock.GetUtcNow();
        var configuration = options.Value;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var lockAcquired = await db
            .Database.SqlQuery<bool>(
                $"SELECT pg_try_advisory_xact_lock({AdvisoryLockKey}) AS \"Value\""
            )
            .SingleAsync(cancellationToken);

        if (!lockAcquired)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new IdempotencyCleanupResult(0, false, Stopwatch.GetElapsedTime(started));
        }

        var abandonedBefore = now.AddMinutes(-configuration.ClaimMinutes);
        var ids = await db
            .IdempotencyRecords.AsNoTracking()
            .Where(record =>
                (
                    (
                        record.Status == IdempotencyStatus.Completed
                        || record.Status == IdempotencyStatus.Failed
                    )
                    && record.ExpiresAt <= now
                )
                || (
                    record.Status == IdempotencyStatus.Processing
                    && record.StartedAt <= abandonedBefore
                )
            )
            .OrderBy(record => record.ExpiresAt)
            .ThenBy(record => record.Id)
            .Select(record => record.Id)
            .Take(configuration.CleanupBatchSize)
            .ToListAsync(cancellationToken);

        var deleted =
            ids.Count == 0
                ? 0
                : await db
                    .IdempotencyRecords.Where(record => ids.Contains(record.Id))
                    .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        var duration = Stopwatch.GetElapsedTime(started);
        logger.LogInformation(
            "Idempotency cleanup deleted {DeletedCount} records in {Duration}",
            deleted,
            duration
        );
        return new IdempotencyCleanupResult(deleted, true, duration);
    }
}
