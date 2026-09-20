using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class DurableBackgroundJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DurableBackgroundJobOptions> options,
    TimeProvider clock,
    ILogger<DurableBackgroundJobWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds), clock);
        try
        {
            await RunOnceAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Durable background job worker stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope
                .ServiceProvider.GetRequiredService<DurableBackgroundJobDispatcher>()
                .DispatchBatchAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            MarketplaceTelemetry.BackgroundJobFailures.Add(1);
            logger.LogError(exception, "Durable background job dispatch cycle failed");
        }
    }
}
