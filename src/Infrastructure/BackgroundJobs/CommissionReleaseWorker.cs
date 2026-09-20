using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class CommissionReleaseWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CommissionReleaseOptions> options,
    ILogger<CommissionReleaseWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuration = options.Value;
        if (!configuration.Enabled)
        {
            logger.LogInformation("Commission release worker is disabled");
            return;
        }

        logger.LogInformation("Commission release worker started");
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(configuration.PollIntervalSeconds)
        );

        try
        {
            await RunOnceAsync(stoppingToken);
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Commission release worker stopped");
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var job = scope.ServiceProvider.GetRequiredService<CommissionReleaseJob>();
            await job.ExecuteAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            MarketplaceTelemetry.BackgroundJobFailures.Add(
                1,
                new KeyValuePair<string, object?>("job.name", "commission_release")
            );
            logger.LogError(exception, "Commission release cycle failed");
            await Task.Delay(
                TimeSpan.FromSeconds(options.Value.FailureBackoffSeconds),
                cancellationToken
            );
        }
    }
}
