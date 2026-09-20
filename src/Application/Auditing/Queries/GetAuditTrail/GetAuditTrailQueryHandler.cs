using modular_mlm.Application.Auditing.Queries.GetAuditTrail.Models;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Auditing.Queries.GetAuditTrail;

public sealed class GetAuditTrailQueryHandler(
    IApplicationDbContext db,
    IUser currentUser,
    IAdministratorAccountService administratorAccounts
) : IRequestHandler<GetAuditTrailQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(
        GetAuditTrailQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administratorAccounts.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();

        var query = db
            .AuditLogs.AsNoTracking()
            .Where(audit => audit.OrganizationId == request.OrganizationId);
        if (request.ActorUserId.HasValue)
            query = query.Where(audit => audit.ActorUserId == request.ActorUserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(audit => audit.Action == request.Action.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(audit => audit.EntityType == request.EntityType.Trim());
        if (request.EntityId.HasValue)
            query = query.Where(audit => audit.EntityId == request.EntityId.Value);
        if (request.From.HasValue)
            query = query.Where(audit => audit.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(audit => audit.CreatedAt <= request.To.Value);

        return await query
            .OrderByDescending(audit => audit.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(audit => new AuditLogDto(
                audit.Id,
                audit.OrganizationId,
                audit.ActorUserId,
                audit.Action,
                audit.EntityType,
                audit.EntityId,
                audit.BeforeJson,
                audit.AfterJson,
                audit.Reason,
                audit.IpAddress,
                audit.UserAgent,
                audit.TraceId,
                audit.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
