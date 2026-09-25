using System.Security.Claims;
using System.Threading.RateLimiting;
using modular_mlm.Infrastructure.Observability;

namespace modular_mlm.Web.Infrastructure.Security;

public static class ApiRateLimitPolicy
{
    public static IServiceCollection AddMarketplaceRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                MarketplaceTelemetry.RateLimitRejections.Add(1);
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(
                            retryAfter.TotalSeconds
                        )
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);
                var factory =
                    context.HttpContext.RequestServices.GetRequiredService<ApiProblemDetailsFactory>();
                var problem = factory.Create(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    ApiErrorCodes.RateLimitExceeded,
                    "Too many requests",
                    "Retry the request after the indicated delay."
                );
                await Results
                    .Json(
                        problem,
                        statusCode: StatusCodes.Status429TooManyRequests,
                        contentType: "application/problem+json"
                    )
                    .ExecuteAsync(context.HttpContext);
            };
            options.AddPolicy(
                RateLimitPolicyNames.Financial,
                context => Fixed(context, 10, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Authentication,
                context => Fixed(
                    context,
                    configuration.GetValue("RateLimiting:Authentication:PermitLimit", 20),
                    TimeSpan.FromMinutes(1)
                )
            );
            options.AddPolicy(
                RateLimitPolicyNames.Invitation,
                context => Fixed(context, 10, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Checkout,
                context => Fixed(context, 10, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Payment,
                context => Fixed(context, 10, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Payout,
                context => Fixed(context, 5, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Refund,
                context => Fixed(context, 5, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.AdministratorFinancialAction,
                context => Fixed(context, 20, TimeSpan.FromMinutes(1))
            );
            options.AddPolicy(
                RateLimitPolicyNames.Webhook,
                context => Fixed(context, 120, TimeSpan.FromMinutes(1))
            );
        });
        return services;
    }

    private static RateLimitPartition<string> Fixed(
        HttpContext context,
        int permitLimit,
        TimeSpan window
    ) =>
        RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }
        );

    private static string PartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationId = context.Request.RouteValues["organizationId"]?.ToString();
        if (userId is not null || organizationId is not null)
            return $"{userId ?? "anonymous"}:{organizationId ?? "global"}";
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
