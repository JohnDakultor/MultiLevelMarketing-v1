namespace modular_mlm.Application.Common.Models;

public sealed record IdentitySecurityAuditEvent(
    Guid OrganizationId,
    Guid SubjectUserId,
    string Action,
    bool Succeeded,
    string? FailureCode,
    DateTimeOffset OccurredAt
);
