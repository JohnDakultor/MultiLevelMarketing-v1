using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Web.Services;

public sealed class HttpCurrentAuthenticationSession(
    IHttpContextAccessor contextAccessor,
    ProtectedCookieService cookies
) : ICurrentAuthenticationSession
{
    internal const string CookieName = "__Host-modular_mlm.session";
    private Guid? _sessionId;
    private bool _read;

    public Guid? SessionId
    {
        get
        {
            if (_read)
                return _sessionId;
            _read = true;
            var context = contextAccessor.HttpContext;
            var value = context is null
                ? null
                : cookies.Read<SessionCookie>(context.Request, CookieName);
            _sessionId = value?.SessionId;
            return _sessionId;
        }
    }

    public void Set(Guid sessionId, TimeSpan lifetime)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("A session identifier is required.", nameof(sessionId));
        var context = GetContext();
        cookies.Write(
            context.Response,
            CookieName,
            new SessionCookie(sessionId),
            lifetime,
            essential: true
        );
        _read = true;
        _sessionId = sessionId;
    }

    public void Clear()
    {
        cookies.Delete(GetContext().Response, CookieName);
        _read = true;
        _sessionId = null;
    }

    private HttpContext GetContext() =>
        contextAccessor.HttpContext
        ?? throw new InvalidOperationException("An HTTP authentication session is required.");

    private sealed record SessionCookie(Guid SessionId);
}
