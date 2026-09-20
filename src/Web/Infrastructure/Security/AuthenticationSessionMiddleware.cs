using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Web.Infrastructure.Security;

public sealed class AuthenticationSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IAuthenticationSessionService sessions,
        ICurrentAuthenticationSession currentSession,
        IOptions<IdentitySecurityOptions> options,
        TimeProvider timeProvider
    )
    {
        if (
            context.User.Identity?.IsAuthenticated != true
            || context.User.Identity.AuthenticationType != IdentityConstants.ApplicationScheme
        )
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            await next(context);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var sessionId = currentSession.SessionId;
        if (sessionId.HasValue)
        {
            var active = await sessions.IsSessionActiveAsync(
                userId,
                sessionId.Value,
                now,
                context.RequestAborted
            );
            if (!active)
            {
                currentSession.Clear();
                await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                // A revoked cookie must not break anonymous storefront/bootstrap or
                // prevent the user from authenticating again. Continue anonymous on
                // endpoints that explicitly allow it; protected endpoints still get
                // a clear 401 response.
                if (context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                    await next(context);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }
        else
        {
            var lifetime = TimeSpan.FromDays(options.Value.RefreshTokenLifetimeDays);
            sessionId = await sessions.CreateOrRotateSessionAsync(
                userId,
                null,
                now,
                now.Add(lifetime),
                context.RequestAborted
            );
            currentSession.Set(sessionId.Value, lifetime);
            var activeSessions = (
                await sessions.GetSessionsAsync(userId, sessionId, context.RequestAborted)
            )
                .Where(session => session.RevokedAt is null && session.ExpiresAt > now)
                .Skip(options.Value.MaximumActiveSessions)
                .ToArray();
            foreach (var excess in activeSessions)
            {
                await sessions.RevokeSessionAsync(
                    userId,
                    excess.Id,
                    now,
                    "active-session-limit",
                    context.RequestAborted
                );
            }
        }

        await next(context);
    }
}
