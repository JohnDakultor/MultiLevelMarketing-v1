namespace modular_mlm.Application.Common.Models;

public sealed record AuditContext(
    string? ActorUserId,
    string? IpAddress,
    string? UserAgent,
    string? TraceId
);

public interface IAuditContextAccessor
{
    AuditContext Current { get; }
}
