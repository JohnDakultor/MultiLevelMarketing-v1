namespace modular_mlm.Application.Common.Models;

public sealed record AuditWriteRequest(
    Guid OrganizationId,
    string Action,
    string EntityType,
    Guid EntityId,
    object? BeforeState,
    object? AfterState,
    string? Reason = null
);
