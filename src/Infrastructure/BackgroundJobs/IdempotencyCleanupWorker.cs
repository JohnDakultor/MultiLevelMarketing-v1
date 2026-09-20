using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Idempotency;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class IdempotencyCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<IdempotencyOptions> options,
    TimeProvider clock,
    ILogger<IdempotencyCleanupWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.CleanupEnabled)
        {
            logger.LogInformation("Idempotency cleanup worker is disabled");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(options.Value.CleanupIntervalMinutes),
            clock
        );

        try
        {
            await RunOnceAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Idempotency cleanup worker stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<IdempotencyCleanupJob>()
                .ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            MarketplaceTelemetry.BackgroundJobFailures.Add(
                1,
                new KeyValuePair<string, object?>("job.name", "idempotency_cleanup")
            );
            logger.LogError(exception, "Idempotency cleanup cycle failed");

            var retryDelay = TimeSpan.FromSeconds(
                Math.Min(60, options.Value.CleanupIntervalMinutes * 60)
            );
            await Task.Delay(retryDelay, clock, cancellationToken);
        }
    }
}
