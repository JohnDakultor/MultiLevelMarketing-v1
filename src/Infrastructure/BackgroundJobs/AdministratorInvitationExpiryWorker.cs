using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class AdministratorInvitationExpiryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AdministratorInvitationExpiryOptions> options,
    ILogger<AdministratorInvitationExpiryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
            return;
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(options.Value.PollIntervalSeconds)
        );
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope
                    .ServiceProvider.GetRequiredService<AdministratorInvitationExpiryJob>()
                    .ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Administrator invitation expiry cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
