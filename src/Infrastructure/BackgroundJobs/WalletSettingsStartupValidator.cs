using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class WalletSettingsStartupValidator(IServiceScopeFactory scopeFactory)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var organizationWithoutSettings = await db.Organizations.AnyAsync(
            organization =>
                !db.WalletSettings.Any(settings => settings.OrganizationId == organization.Id),
            cancellationToken
        );
        if (organizationWithoutSettings)
            throw new InvalidOperationException(
                "Every organization must have wallet availability settings."
            );

        var invalidSettings = await db.WalletSettings.AnyAsync(
            settings =>
                settings.CommissionReleaseTrigger < CommissionReleaseTrigger.PaymentConfirmed
                || settings.CommissionReleaseTrigger > CommissionReleaseTrigger.ReturnWindowElapsed
                || settings.ReleaseDelayDays < 0
                || settings.ReleaseDelayDays > 365
                || settings.ReturnWindowDays < 0
                || settings.ReturnWindowDays > 365
                || settings.MinimumPayoutAmount <= 0m
                || settings.MaximumNegativeBalance < 0m
                || (
                    !settings.AllowNegativeRecoverableBalance
                    && settings.MaximumNegativeBalance != 0m
                ),
            cancellationToken
        );
        if (invalidSettings)
            throw new InvalidOperationException(
                "One or more organizations have invalid wallet availability settings."
            );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
