using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger.Models;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class BinaryVolumeLedger : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/agents/{agentId:guid}/finance";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group.RequireRateLimiting(RateLimitPolicyNames.Financial);
        group.MapGet(GetEntries, "binary-volume/entries");
    }

    public static async Task<Ok<BinaryVolumeLedgerPageDto>> GetEntries(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int page = 1,
        int pageSize = 20,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        PlacementSide? side = null,
        BinaryVolumeEntryType? entryType = null
    ) => TypedResults.Ok(await sender.Send(new GetBinaryVolumeLedgerQuery(
        organizationId, agentId, page, pageSize, from, to, side, entryType
    )));
}
