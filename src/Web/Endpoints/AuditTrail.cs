using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Auditing.Queries.GetAuditTrail;
using modular_mlm.Application.Auditing.Queries.GetAuditTrail.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class AuditTrail : IEndpointGroup
{
    public static string RoutePrefix =>
        "/api/organizations/{organizationId:guid}/admin/audit-trail";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetAuditTrail);
    }

    public static async Task<Ok<IReadOnlyList<AuditLogDto>>> GetAuditTrail(
        ISender sender,
        Guid organizationId,
        Guid? actorUserId = null,
        string? action = null,
        string? entityType = null,
        Guid? entityId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 1,
        int pageSize = 50
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAuditTrailQuery(
                    organizationId,
                    actorUserId,
                    action,
                    entityType,
                    entityId,
                    from,
                    to,
                    page,
                    pageSize
                )
            )
        );
}
