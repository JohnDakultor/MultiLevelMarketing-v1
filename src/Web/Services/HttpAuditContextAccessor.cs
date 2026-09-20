using modular_mlm.Application.Common.Models;

namespace modular_mlm.Web.Services;

public sealed class HttpAuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    : IAuditContextAccessor
{
    public AuditContext Current
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            return new AuditContext(
                context?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                context?.Connection.RemoteIpAddress?.ToString(),
                context?.Request.Headers.UserAgent.ToString(),
                context?.TraceIdentifier
            );
        }
    }
}
