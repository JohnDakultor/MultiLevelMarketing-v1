using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    TimeProvider clock,
    ILogger<NotificationDeliveryWorker> logger
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
            logger.LogInformation("Notification delivery worker stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var result = await scope
                .ServiceProvider.GetRequiredService<NotificationDeliveryJob>()
                .ExecuteAsync(cancellationToken);
            if (result.DeadLettered > 0)
                MarketplaceTelemetry.BackgroundJobFailures.Add(
                    result.DeadLettered,
                    new KeyValuePair<string, object?>("job.name", "notification_delivery")
                );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            MarketplaceTelemetry.BackgroundJobFailures.Add(1);
            logger.LogError(exception, "Notification delivery cycle failed");
        }
    }
}
