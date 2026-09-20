namespace modular_mlm.Application.Auditing.Queries.GetAuditTrail.Models;

public sealed record AuditLogDto(
    Guid Id,
    Guid OrganizationId,
    Guid ActorUserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? Reason,
    string? IpAddress,
    string? UserAgent,
    string? TraceId,
    DateTimeOffset CreatedAt
);
