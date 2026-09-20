using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class BinaryPairingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<BinaryPairingWorker> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Binary pairing worker started");

        using var timer = new PeriodicTimer(CheckInterval);

        try
        {
            await ProcessDueSchedulesAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ProcessDueSchedulesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Binary pairing worker stopped");
        }
    }

    private async Task ProcessDueSchedulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor =
                scope.ServiceProvider.GetRequiredService<BinaryPairingScheduleProcessor>();

            await processor.ProcessDueSchedulesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while checking binary pairing schedules");
        }
    }
}
