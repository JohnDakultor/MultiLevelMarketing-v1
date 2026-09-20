using Domain.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Domain.Services;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public static class CommissionReleaseRegistration
{
    public static IServiceCollection AddCommissionRelease(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<CommissionReleaseOptions>()
            .Bind(configuration.GetSection(CommissionReleaseOptions.SectionName))
            .Validate(
                options => options.PollIntervalSeconds is >= 5 and <= 86_400,
                "Commission release interval must be between 5 and 86400 seconds."
            )
            .Validate(
                options => options.OrganizationBatchSize is >= 1 and <= 500,
                "Organization batch size must be between 1 and 500."
            )
            .Validate(
                options => options.CommissionBatchSize is >= 1 and <= 500,
                "Commission batch size must be between 1 and 500."
            )
            .Validate(
                options => options.FailureBackoffSeconds is >= 1 and <= 3_600,
                "Failure backoff must be between 1 and 3600 seconds."
            )
            .ValidateOnStart();

        services.AddSingleton<CommissionAvailabilityPolicy>();
        services.AddSingleton<WalletBalanceCalculator>();
        services.AddSingleton<CommissionReleaseCursor>();
        services.AddScoped<CommissionReleaseJob>();
        services.AddHostedService<WalletSettingsStartupValidator>();
        services.AddHostedService<CommissionReleaseWorker>();
        return services;
    }
}
