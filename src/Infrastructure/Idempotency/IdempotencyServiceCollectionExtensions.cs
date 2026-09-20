using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Infrastructure.Idempotency;

public static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotencyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .Validate(IdempotencyOptions.IsValid, "Idempotency configuration is invalid.")
            .ValidateOnStart();
        services.AddDataProtection();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IdempotencyCleanupJob>();
        services.AddHostedService<IdempotencyCleanupWorker>();
        return services;
    }
}
