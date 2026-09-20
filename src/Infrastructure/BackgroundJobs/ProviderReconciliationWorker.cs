using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Commands.ReconcileProviderState;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class ProviderReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<ProviderReconciliationWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var changes = await sender.Send(new ReconcileProviderStateCommand(), stoppingToken);
                if (changes > 0)
                    logger.LogInformation(
                        "Provider reconciliation applied {ChangeCount} status changes",
                        changes
                    );
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Provider reconciliation failed");
            }
        }
    }
}
