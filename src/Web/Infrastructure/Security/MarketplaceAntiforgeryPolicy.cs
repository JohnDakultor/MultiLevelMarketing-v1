using Microsoft.AspNetCore.Antiforgery;

namespace modular_mlm.Web.Infrastructure.Security;

public static class MarketplaceAntiforgeryPolicy
{
    public const string HeaderName = "X-CSRF-TOKEN";
    private const string ProductionCookieName = "__Host-modular_mlm.csrf";
    private const string DevelopmentCookieName = "modular_mlm.csrf";

    public static IServiceCollection AddMarketplaceAntiforgery(
        this IServiceCollection services,
        IHostEnvironment environment
    )
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = environment.IsProduction()
                ? ProductionCookieName
                : DevelopmentCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
            options.Cookie.SecurePolicy = environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
        services.AddScoped<RequireAntiforgeryForCookieFilter>();
        return services;
    }
}
