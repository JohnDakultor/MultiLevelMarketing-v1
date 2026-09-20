using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Identity;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireRateLimiting(RateLimitPolicyNames.Authentication);
        groupBuilder.AddEndpointFilter<IdentitySecurityAuditEndpointFilter>();
        groupBuilder.MapIdentityApi<ApplicationUser>();

        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Logout(
        SignInManager<ApplicationUser> signInManager,
        IUser currentUser,
        ICurrentAuthenticationSession currentSession,
        IAuthenticationSessionService sessions,
        TimeProvider timeProvider,
        [FromBody] object empty,
        CancellationToken cancellationToken
    )
    {
        if (empty != null)
        {
            if (
                !string.IsNullOrWhiteSpace(currentUser.Id)
                && currentSession.SessionId is { } sessionId
            )
            {
                await sessions.RevokeSessionAsync(
                    currentUser.Id,
                    sessionId,
                    timeProvider.GetUtcNow(),
                    "signed-out",
                    cancellationToken
                );
                currentSession.Clear();
            }
            await signInManager.SignOutAsync();
            return TypedResults.Ok();
        }

        return TypedResults.Unauthorized();
    }
}
