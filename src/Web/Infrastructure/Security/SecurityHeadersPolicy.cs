namespace modular_mlm.Web.Infrastructure.Security;

public static class SecurityHeadersPolicy
{
    public static IApplicationBuilder UseMarketplaceSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(
            async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    var isScalarDocumentation = context.Request.Path.StartsWithSegments(
                        "/scalar",
                        StringComparison.OrdinalIgnoreCase
                    );
                    headers.TryAdd("X-Content-Type-Options", "nosniff");
                    headers.TryAdd("Referrer-Policy", "no-referrer");
                    headers.TryAdd("X-Frame-Options", "DENY");
                    headers.TryAdd(
                        "Content-Security-Policy",
                        isScalarDocumentation
                            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'"
                            : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'"
                    );
                    headers.TryAdd(
                        "Permissions-Policy",
                        "camera=(), microphone=(), geolocation=()"
                    );
                    return Task.CompletedTask;
                });
                await next();
            }
        );
}
