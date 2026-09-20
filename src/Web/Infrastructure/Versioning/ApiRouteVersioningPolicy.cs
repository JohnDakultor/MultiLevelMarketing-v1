namespace modular_mlm.Web.Infrastructure.Versioning;

public static class ApiRouteVersioningPolicy
{
    public const string CurrentVersion = "1.0";
    public const string VersionHeader = "Api-Version";
    public const string SupportedVersionsHeader = "Api-Supported-Versions";

    public static IApplicationBuilder UseMarketplaceApiVersioning(this IApplicationBuilder app) =>
        app.Use(
            async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers.TryAdd(VersionHeader, CurrentVersion);
                        context.Response.Headers.TryAdd(SupportedVersionsHeader, CurrentVersion);
                        return Task.CompletedTask;
                    });
                }

                await next(context);
            }
        );
}
