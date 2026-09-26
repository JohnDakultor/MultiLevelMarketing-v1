namespace modular_mlm.Web.Infrastructure.Tenancy;

public sealed class OrganizationResolutionOptions
{
    public const string SectionName = "OrganizationResolution";

    public bool Enabled { get; init; } = true;
    public bool RequireKnownHost { get; init; } = true;
    public string? FallbackOrganizationSlug { get; init; }
    public string[] DevelopmentHosts { get; init; } = ["localhost", "127.0.0.1", "::1"];
    public string[] BypassPathPrefixes { get; init; } =
    [
        "/alive",
        "/health",
        "/openapi",
        "/scalar",
        "/api/Users",
        "/api/me",
        "/api/administrator-invitations",
        "/api/organizations",
        "/api/paymongo",
    ];
}

public static class OrganizationResolutionExtensions
{
    public static IServiceCollection AddOrganizationResolution(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<OrganizationResolutionOptions>()
            .Bind(configuration.GetSection(OrganizationResolutionOptions.SectionName))
            .Validate(
                options => options.DevelopmentHosts.All(host => !string.IsNullOrWhiteSpace(host)),
                "Development hosts cannot contain blank values."
            )
            .Validate(
                options =>
                    string.IsNullOrWhiteSpace(options.FallbackOrganizationSlug)
                    || System.Text.RegularExpressions.Regex.IsMatch(
                        options.FallbackOrganizationSlug,
                        "^[a-z0-9]+(?:-[a-z0-9]+)*$"
                    ),
                "The fallback organization slug is invalid."
            )
            .ValidateOnStart();
        services.AddScoped<ResolvedOrganizationContext>();
        return services;
    }

    public static IApplicationBuilder UseOrganizationResolution(
        this IApplicationBuilder application
    ) => application.UseMiddleware<OrganizationResolutionMiddleware>();
}
