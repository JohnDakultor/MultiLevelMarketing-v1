using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Identity.Commands.RevokeCurrentSession;
using modular_mlm.Application.Identity.Queries.GetAuthenticationSessions;
using modular_mlm.Application.Identity.Queries.GetCurrentUser;
using modular_mlm.Application.Identity.Queries.GetCurrentUser.Models;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Web.Endpoints;

public sealed class CurrentIdentity : IEndpointGroup
{
    public static string RoutePrefix => "/api/me";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetCurrentUser).AllowAnonymous();
        group.MapGet(GetSessions, "sessions").RequireAuthorization();
        group.MapPost(RevokeCurrentSession, "session/revoke").RequireAuthorization();
    }

    public static async Task<Results<Ok<CurrentUserDto>, NoContent>> GetCurrentUser(
        HttpContext context,
        ISender sender
    )
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return TypedResults.NoContent();

        return TypedResults.Ok(await sender.Send(new GetCurrentUserQuery()));
    }

    public static async Task<NoContent> RevokeCurrentSession(
        ISender sender,
        ICurrentAuthenticationSession currentSession,
        SignInManager<ApplicationUser> signInManager,
        RevokeCurrentSessionRequest request
    )
    {
        var revokesCurrent = currentSession.SessionId == request.SessionId;
        await sender.Send(new RevokeCurrentSessionCommand(request.SessionId));
        if (revokesCurrent)
        {
            currentSession.Clear();
            await signInManager.SignOutAsync();
        }
        return TypedResults.NoContent();
    }

    public static async Task<Ok<IReadOnlyList<AuthenticationSessionInfo>>> GetSessions(
        ISender sender
    ) => TypedResults.Ok(await sender.Send(new GetAuthenticationSessionsQuery()));
}

public sealed record RevokeCurrentSessionRequest(Guid SessionId);
