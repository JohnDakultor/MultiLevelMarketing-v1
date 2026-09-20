using modular_mlm.Application.Auditing.Queries.GetAuditTrail.Models;

namespace modular_mlm.Application.Auditing.Queries.GetAuditTrail;

public sealed record GetAuditTrailQuery(
    Guid OrganizationId,
    Guid? ActorUserId = null,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<IReadOnlyList<AuditLogDto>>, IOrganizationAdminRequest;
