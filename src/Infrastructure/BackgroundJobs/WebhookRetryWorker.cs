using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Commerce.Commands.ReconcilePaymentByProvider;
using modular_mlm.Application.Payouts.Commands.ReconcilePayoutByTransfer;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class WebhookRetryWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<WebhookRetryWorker> logger
) : BackgroundService
{
    private const int MaximumAttempts = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ProcessBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Webhook retry worker stopped");
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var now = clock.GetUtcNow();
        var attempts = await db
            .WebhookProcessingAttempts.Where(attempt =>
                (attempt.Status == "Pending" || attempt.Status == "Failed")
                && attempt.NextAttemptAt <= now
            )
            .OrderBy(attempt => attempt.NextAttemptAt)
            .Take(25)
            .ToListAsync(cancellationToken);
        foreach (var attempt in attempts)
        {
            try
            {
                if (attempt.ResourceKind == "Payment")
                    await sender.Send(
                        new ReconcilePaymentByProviderCommand(attempt.ProviderResourceId),
                        cancellationToken
                    );
                else if (attempt.ResourceKind == "Payout")
                    await sender.Send(
                        new ReconcilePayoutByTransferCommand(attempt.ProviderResourceId),
                        cancellationToken
                    );
                else
                    throw new InvalidOperationException("Unsupported webhook resource kind.");
                attempt.MarkSucceeded(clock.GetUtcNow());
                MarketplaceTelemetry.WebhookRetries.Add(1);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                attempt.MarkFailed(exception.Message, clock.GetUtcNow(), MaximumAttempts);
                logger.LogWarning(
                    exception,
                    "Webhook event {ProviderEventId} retry failed",
                    attempt.ProviderEventId
                );
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
