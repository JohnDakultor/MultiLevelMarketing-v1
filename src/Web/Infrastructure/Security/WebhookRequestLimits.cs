using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace modular_mlm.Web.Infrastructure.Security;

public static class WebhookRequestLimits
{
    public const long MaximumBodySize = 256 * 1_024;

    public static RouteGroupBuilder ApplyWebhookLimits(this RouteGroupBuilder group)
    {
        group.RequireRateLimiting(RateLimitPolicyNames.Webhook);
        group.WithMetadata(new RequestSizeLimitAttribute(MaximumBodySize));
        group.WithRequestTimeout(TimeSpan.FromSeconds(15));
        group.AddEndpointFilter(
            async (context, next) =>
            {
                var request = context.HttpContext.Request;
                if (!request.HasJsonContentType())
                    return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
                if (request.ContentLength > MaximumBodySize)
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
                return await next(context);
            }
        );
        return group;
    }
}
