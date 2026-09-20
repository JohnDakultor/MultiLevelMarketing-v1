using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Email;

namespace modular_mlm.Infrastructure.Notifications;

public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<NotificationDeliveryOptions>()
            .Bind(configuration.GetSection(NotificationDeliveryOptions.SectionName))
            .Validate(
                NotificationDeliveryOptions.IsValid,
                "Notification delivery settings are invalid."
            )
            .ValidateOnStart();
        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName));
        // MapIdentityApi resolves its IEmailSender<TUser> while endpoints are mapped
        // from the root provider. The transport is stateless and only consumes
        // singleton IOptions, so it must also be safe to resolve from the root.
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<NotificationTemplateRenderer>();
        services.AddScoped<INotificationRecipientResolver, IdentityNotificationRecipientResolver>();
        services.AddScoped<INotificationDeliveryOutbox, NotificationDeliveryOutbox>();
        services.AddScoped<IInvitationDeliveryOutbox, AdministratorInvitationDeliveryOutbox>();
        services.AddScoped<INotificationSender, EmailNotificationSender>();
        services.AddScoped<NotificationDeliveryJob>();
        services.AddHostedService<NotificationDeliveryWorker>();
        return services;
    }
}
