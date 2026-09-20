using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Inventory.Commands.ReleaseExpiredInventory;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed record InventoryReservationCleanupResult(
    int OrganizationsProcessed,
    int ReservationsProcessed,
    int ReservationsReleased,
    int Failures,
    TimeSpan Duration,
    TimeSpan? OldestLag
);

public sealed class InventoryReservationCleanupJob(
    IApplicationDbContext db,
    IServiceScopeFactory scopeFactory,
    IOptions<InventoryReservationCleanupOptions> options,
    TimeProvider timeProvider,
    ILogger<InventoryReservationCleanupJob> logger
)
{
    public async Task<InventoryReservationCleanupResult> ExecuteAsync(
        CancellationToken cancellationToken
    )
    {
        var started = Stopwatch.GetTimestamp();
        var now = timeProvider.GetUtcNow();
        var configuration = options.Value;
        var oldestExpiry = await db
            .InventoryReservations.AsNoTracking()
            .Where(reservation => reservation.ExpiresAt <= now)
            .OrderBy(reservation => reservation.ExpiresAt)
            .Select(reservation => (DateTimeOffset?)reservation.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
        var processed = 0;
        var released = 0;
        var failures = 0;
        var organizationsProcessed = 0;
        Guid? lastOrganizationId = null;

        while (true)
        {
            var organizationIds = await db
                .Organizations.AsNoTracking()
                .Where(organization =>
                    organization.Status == OrganizationStatus.Active
                    && (!lastOrganizationId.HasValue || organization.Id > lastOrganizationId.Value)
                )
                .OrderBy(organization => organization.Id)
                .Select(organization => organization.Id)
                .Take(configuration.OrganizationBatchSize)
                .ToListAsync(cancellationToken);
            if (organizationIds.Count == 0)
                break;

            foreach (var organizationId in organizationIds)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    var result = await sender.Send(
                        new ReleaseExpiredInventoryCommand(
                            organizationId,
                            configuration.ReservationBatchSize,
                            now
                        ),
                        cancellationToken
                    );
                    organizationsProcessed++;
                    processed += result.ProcessedCount;
                    released += result.ReleasedCount;
                    failures += result.FailureCount;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    failures++;
                    logger.LogError(
                        exception,
                        "Inventory cleanup failed for organization {OrganizationId}",
                        organizationId
                    );
                }
            }

            lastOrganizationId = organizationIds[^1];
        }

        var duration = Stopwatch.GetElapsedTime(started);
        logger.LogInformation(
            "Inventory cleanup processed {Processed} reservations and released {Released} in {Duration}; failures: {Failures}",
            processed,
            released,
            duration,
            failures
        );
        return new InventoryReservationCleanupResult(
            organizationsProcessed,
            processed,
            released,
            failures,
            duration,
            oldestExpiry.HasValue ? now - oldestExpiry.Value : null
        );
    }
}
