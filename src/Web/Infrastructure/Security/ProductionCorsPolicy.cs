namespace modular_mlm.Web.Infrastructure.Security;

public static class ProductionCorsPolicy
{
    public static IServiceCollection AddMarketplaceCors(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(child => child.Value?.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
        if (origins.Any(origin => origin == "*"))
            throw new InvalidOperationException(
                "Wildcard CORS origins cannot be used with credentialed requests."
            );

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
            {
                if (origins.Length > 0)
                    policy
                        .WithOrigins(origins)
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                        .WithHeaders(
                            "Authorization",
                            "Content-Type",
                            "Accept",
                            "Paymongo-Signature",
                            "X-Paymongo-Signature"
                        )
                        .AllowCredentials();
            })
        );
        return services;
    }
}
