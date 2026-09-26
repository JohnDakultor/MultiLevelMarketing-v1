using Microsoft.Extensions.Options;
using modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost;

namespace modular_mlm.Web.Infrastructure.Tenancy;

public sealed class OrganizationResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ISender sender,
        ResolvedOrganizationContext organizationContext,
        IOptions<OrganizationResolutionOptions> options
    )
    {
        var settings = options.Value;
        var hostName = httpContext.Request.Host.Host;
        if (!settings.Enabled || IsDevelopmentHost(hostName, settings))
        {
            await next(httpContext);
            return;
        }

        var resolved = await sender.Send(
            new ResolveOrganizationByHostQuery(hostName, settings.FallbackOrganizationSlug),
            httpContext.RequestAborted
        );
        if (resolved is not null)
        {
            organizationContext.Initialize(resolved);
            await next(httpContext);
            return;
        }

        if (!settings.RequireKnownHost || IsBypassedPath(httpContext.Request.Path, settings))
        {
            await next(httpContext);
            return;
        }

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(
            new { title = "Organization not found", status = StatusCodes.Status404NotFound },
            httpContext.RequestAborted
        );
    }

    private static bool IsDevelopmentHost(string hostName, OrganizationResolutionOptions options)
    {
        if (options.DevelopmentHosts.Contains(hostName, StringComparer.OrdinalIgnoreCase))
            return true;

        // RFC 6761 reserves localhost and every name below it for loopback use.
        // Aspire uses service-specific names such as webapi.dev.localhost.
        if (
            hostName.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || hostName.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
        )
            return true;

        return System.Net.IPAddress.TryParse(hostName, out var address)
            && System.Net.IPAddress.IsLoopback(address);
    }

    private static bool IsBypassedPath(PathString path, OrganizationResolutionOptions options) =>
        options.BypassPathPrefixes.Any(prefix =>
            path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase)
        );
}
