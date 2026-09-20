using System.Security.Cryptography;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Web.Services;

public sealed class HttpCartSessionAccessor(
    IHttpContextAccessor httpContextAccessor,
    ProtectedCookieService cookies
) : ICartSessionAccessor
{
    private const string CookieName = "__Host-modular_mlm.cart";
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(30);
    private string? _sessionId;

    public string GetOrCreateSessionId()
    {
        var existingSessionId = GetSessionId();
        if (existingSessionId is not null)
            return existingSessionId;

        var context = GetHttpContext();
        _sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cookies.Write(
            context.Response,
            CookieName,
            new CartSessionCookie(_sessionId),
            CookieLifetime,
            essential: true
        );

        return _sessionId;
    }

    public string? GetSessionId()
    {
        if (_sessionId is not null)
            return _sessionId;

        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return null;

        var cookie = cookies.Read<CartSessionCookie>(context.Request, CookieName);
        _sessionId = string.IsNullOrWhiteSpace(cookie?.SessionId) ? null : cookie.SessionId;
        return _sessionId;
    }

    public void ClearSession()
    {
        cookies.Delete(GetHttpContext().Response, CookieName);
        _sessionId = null;
    }

    private HttpContext GetHttpContext() =>
        httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("A cart session requires an active HTTP request.");

    private sealed record CartSessionCookie(string SessionId);
}
