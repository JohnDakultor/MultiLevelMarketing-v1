using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Network.Commands.AdminMoveUncommittedPlacement;
using modular_mlm.Application.Network.Queries.GetAdminAgentDetails;
using modular_mlm.Application.Network.Queries.GetAdminAgentDetails.Models;
using modular_mlm.Application.Network.Queries.GetAdminAgents;
using modular_mlm.Application.Network.Queries.GetAdminAgents.Models;
using modular_mlm.Application.Network.Queries.GetAgentApplications;
using modular_mlm.Application.Network.Queries.GetAgentApplications.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Web.Contracts.Agents;

namespace modular_mlm.Web.Endpoints;

public sealed class AdminAgentOperations : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/agents";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetApplications, "applications");
        group.MapGet(GetAgents);
        group.MapGet(GetAgentDetails, "{agentId:guid}");
        group.MapPost(MoveUncommittedPlacement, "{agentId:guid}/move-uncommitted-placement");
    }

    public static async Task<Ok<AgentApplicationsPageDto>> GetApplications(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        AgentStatus? status = null,
        string? search = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAgentApplicationsQuery(organizationId, page, pageSize, status, search)
            )
        );

    public static async Task<Ok<AdminAgentsPageDto>> GetAgents(
        ISender sender,
        Guid organizationId,
        int page = 1,
        int pageSize = 20,
        AgentStatus? status = null,
        AgentPlacementFilter placement = AgentPlacementFilter.All,
        string? search = null
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new GetAdminAgentsQuery(organizationId, page, pageSize, status, placement, search)
            )
        );

    public static async Task<Results<Ok<AdminAgentDetailsDto>, NotFound>> GetAgentDetails(
        ISender sender,
        Guid organizationId,
        Guid agentId
    )
    {
        var details = await sender.Send(new GetAdminAgentDetailsQuery(organizationId, agentId));
        return details is null ? TypedResults.NotFound() : TypedResults.Ok(details);
    }

    public static async Task<NoContent> MoveUncommittedPlacement(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        MoveUncommittedPlacementRequest request
    )
    {
        await sender.Send(
            new AdminMoveUncommittedPlacementCommand(
                organizationId,
                agentId,
                request.NewParentAgentId,
                request.NewSide,
                request.ExpectedCurrentParentAgentId,
                request.ExpectedCurrentSide,
                request.Reason
            )
        );
        return TypedResults.NoContent();
    }
}
