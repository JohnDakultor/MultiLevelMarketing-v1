using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public static class InventoryReservationRegistration
{
    public static IServiceCollection AddInventoryReservationCleanup(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<InventoryReservationCleanupOptions>()
            .Bind(configuration.GetSection(InventoryReservationCleanupOptions.SectionName))
            .Validate(
                options => options.PollIntervalSeconds is >= 5 and <= 86_400,
                "Invalid cleanup interval."
            )
            .Validate(
                options => options.OrganizationBatchSize is >= 1 and <= 500,
                "Invalid organization batch size."
            )
            .Validate(
                options => options.ReservationBatchSize is >= 1 and <= 500,
                "Invalid reservation batch size."
            )
            .Validate(
                options => options.ReservationLifetimeMinutes is >= 1 and <= 1_440,
                "Invalid reservation lifetime."
            )
            .ValidateOnStart();
        services.AddSingleton<IInventoryReservationSettings, InventoryReservationSettings>();
        services.AddScoped<InventoryReservationCleanupJob>();
        services.AddHostedService<InventoryReservationCleanupWorker>();
        return services;
    }
}
