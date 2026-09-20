using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Infrastructure.Messaging;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddReliableOutboxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<OutboxDispatchOptions>()
            .Bind(configuration.GetSection(OutboxDispatchOptions.SectionName))
            .Validate(OutboxDispatchOptions.IsValid, "Outbox dispatch settings are invalid.")
            .ValidateOnStart();
        services.AddSingleton<OutboxConsumerRegistry>();
        services.AddScoped<OutboxDispatcher>();
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped<IDeadLetterMessageStore, OutboxDeadLetterMessageStore>();
        services.AddScoped<OutboxDeadLetterAlertSender>();
        services.AddHostedService<OutboxWorker>();
        return services;
    }
}
