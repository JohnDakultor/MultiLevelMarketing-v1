using System.Diagnostics;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Web.Infrastructure.Observability;

public sealed class ApiTelemetryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var route = context.GetEndpoint() is RouteEndpoint endpoint
                ? endpoint.RoutePattern.RawText
                : "unmatched";
            MarketplaceTelemetry.ApiRequestDuration.Record(
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                new TagList
                {
                    { "http.request.method", context.Request.Method },
                    { "http.route", route },
                    { "http.response.status_code", context.Response.StatusCode },
                }
            );
        }
    }
}
