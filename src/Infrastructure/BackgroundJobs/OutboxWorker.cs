using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Messaging;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxDispatchOptions> options,
    TimeProvider clock,
    ILogger<OutboxWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.PollSeconds),
            clock
        );
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var started = Stopwatch.GetTimestamp();
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                    var count = await dispatcher.DispatchBatchAsync(
                        options.Value.BatchSize,
                        stoppingToken
                    );
                    MarketplaceTelemetry.OutboxMessagesProcessed.Add(count);
                    MarketplaceTelemetry.OutboxDispatchDuration.Record(
                        Stopwatch.GetElapsedTime(started).TotalMilliseconds
                    );
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    MarketplaceTelemetry.BackgroundJobFailures.Add(1);
                    logger.LogError(exception, "Outbox dispatch cycle failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Outbox worker stopped");
        }
    }
}
