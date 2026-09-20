using Microsoft.Net.Http.Headers;

namespace modular_mlm.Web.Infrastructure.Security;

public static class AuthenticationTransportPolicy
{
    public static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method)
        || HttpMethods.IsHead(method)
        || HttpMethods.IsOptions(method)
        || HttpMethods.IsTrace(method);

    public static bool HasBearerAuthorization(HttpRequest request) =>
        request.Headers.TryGetValue(HeaderNames.Authorization, out var values)
        && values.Any(value =>
            value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
        );

    public static bool IsProviderWebhook(PathString path) =>
        path.StartsWithSegments("/api/webhooks", StringComparison.OrdinalIgnoreCase);

    public static bool RequiresAntiforgery(HttpContext context) =>
        !IsSafeMethod(context.Request.Method)
        && context.User.Identity?.IsAuthenticated == true
        && !HasBearerAuthorization(context.Request)
        && !IsProviderWebhook(context.Request.Path);
}
