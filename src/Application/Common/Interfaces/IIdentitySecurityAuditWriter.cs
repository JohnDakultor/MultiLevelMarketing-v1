using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IIdentitySecurityAuditWriter
{
    Task WriteAsync(IdentitySecurityAuditEvent securityEvent, CancellationToken cancellationToken);
}
