using Microsoft.AspNetCore.Antiforgery;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class AntiforgeryTokens : IEndpointGroup
{
    public static string RoutePrefix => "/api/security";

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet(GetToken, "antiforgery-token").RequireAuthorization();

    public static IResult GetToken(HttpContext context, IAntiforgery antiforgery)
    {
        if (AuthenticationTransportPolicy.HasBearerAuthorization(context.Request))
            return Results.BadRequest(new { errorCode = "cookie_authentication_required" });
        var tokens = antiforgery.GetAndStoreTokens(context);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(
            new AntiforgeryTokenResponse(
                tokens.RequestToken ?? throw new InvalidOperationException("Token unavailable."),
                tokens.HeaderName ?? MarketplaceAntiforgeryPolicy.HeaderName
            )
        );
    }
}

public sealed record AntiforgeryTokenResponse(string RequestToken, string HeaderName);
