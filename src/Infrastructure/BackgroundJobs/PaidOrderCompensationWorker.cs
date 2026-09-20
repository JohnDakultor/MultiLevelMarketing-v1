using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Compensation.Commands.ProcessOutstandingPaidOrderCompensation;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class PaidOrderCompensationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PaidOrderCompensationWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    await sender.Send(
                        new ProcessOutstandingPaidOrderCompensationCommand(),
                        stoppingToken
                    );
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "The paid-order compensation retry cycle failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Paid-order compensation worker stopped");
        }
    }
}
