using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Infrastructure.Identity;

public static class IdentitySecurityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentitySecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var section = configuration.GetSection(IdentitySecurityOptions.SectionName);
        var configured = section.Get<IdentitySecurityOptions>() ?? new IdentitySecurityOptions();
        services
            .AddOptions<IdentitySecurityOptions>()
            .Bind(section)
            .Validate(IdentitySecurityOptions.IsValid, "Identity security settings are invalid.")
            .Validate(
                options => !environment.IsProduction() || options.RequireAdministratorMfa,
                "Administrator MFA must be required in Production."
            )
            .ValidateOnStart();
        services.Configure<IdentityOptions>(options =>
            options.SignIn.RequireConfirmedEmail = configured.RequireConfirmedEmail
        );
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-modular_mlm.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(configured.RefreshTokenLifetimeDays);
        });
        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                IdentitySecurityOptions.AdministratorMfaPolicy,
                policy =>
                    policy
                        .RequireRole(Roles.Administrator)
                        .RequireAuthenticatedUser()
                        .RequireClaim("amr", "mfa")
            );
        services.AddScoped<IAuthenticationSessionService, AuthenticationSessionService>();
        services.AddScoped<ICurrentIdentityReader, CurrentIdentityReader>();
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();
        return services;
    }
}
