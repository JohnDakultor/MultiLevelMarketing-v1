using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Network.Commands.ActivateAgent;
using modular_mlm.Application.Network.Commands.ApplyAsAgent;
using modular_mlm.Application.Network.Commands.ApproveAgent;
using modular_mlm.Application.Network.Commands.AutoPlaceAgent;
using modular_mlm.Application.Network.Commands.PlaceAgent;
using modular_mlm.Application.Network.Commands.ReactivateAgent;
using modular_mlm.Application.Network.Commands.RejectAgent;
using modular_mlm.Application.Network.Commands.SuspendAgent;
using modular_mlm.Application.Network.Queries.GetAgentLegSummary;
using modular_mlm.Application.Network.Queries.GetAgentLegSummary.Models;
using modular_mlm.Application.Network.Queries.GetBinaryTree;
using modular_mlm.Application.Network.Queries.GetBinaryTree.Models;
using modular_mlm.Application.Network.Queries.GetDirectPlacementChildren;
using modular_mlm.Application.Network.Queries.GetDirectPlacementChildren.Models;
using modular_mlm.Application.Network.Queries.GetDirectRecruits;
using modular_mlm.Application.Network.Queries.GetDirectRecruits.Models;
using modular_mlm.Application.Network.Queries.GetDownline;
using modular_mlm.Application.Network.Queries.GetDownline.Models;
using modular_mlm.Application.Network.Queries.GetPlacementAncestors;
using modular_mlm.Application.Network.Queries.GetPlacementAncestors.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Web.Endpoints;

public sealed class Agents : IEndpointGroup
{
    public static string? RoutePrefix => "/api/organizations/{organizationId:guid}/agents";

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(Apply, "applications").RequireAuthorization();
        group
            .MapPost(Place, "{agentId:guid}/placement")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(AutoPlace, "{agentId:guid}/auto-placement")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(Approve, "{agentId:guid}/approve")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(Reject, "{agentId:guid}/reject")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(Activate, "{agentId:guid}/activate")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(Suspend, "{agentId:guid}/suspend")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapPost(Reactivate, "{agentId:guid}/reactivate")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group
            .MapGet(GetTree, "{agentId:guid}/network/tree")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetDownline, "{agentId:guid}/network/downline")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetChildren, "{agentId:guid}/network/children")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetAncestors, "{agentId:guid}/network/ancestors")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetRecruits, "{agentId:guid}/network/recruits")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
        group
            .MapGet(GetLegSummary, "{agentId:guid}/network/leg-summary")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator, Roles.Agent));
    }

    public static async Task<Created<Guid>> Apply(
        ISender sender,
        Guid organizationId,
        ApplyAsAgentRequest request
    )
    {
        var id = await sender.Send(new ApplyAsAgentCommand(organizationId, request.SponsorAgentId));
        return TypedResults.Created($"/api/organizations/{organizationId}/agents/{id}", id);
    }

    public static async Task<NoContent> Place(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        PlaceAgentRequest request
    )
    {
        await sender.Send(
            new PlaceAgentCommand(organizationId, agentId, request.ParentAgentId, request.Side)
        );
        return TypedResults.NoContent();
    }

    public static async Task<Ok<PlacementDecision>> AutoPlace(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        AutoPlaceAgentRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new AutoPlaceAgentCommand(
                    organizationId,
                    agentId,
                    request.Strategy,
                    request.PreferredSide
                )
            )
        );

    public static async Task<Ok<IReadOnlyList<BinaryTreeNodeDto>>> GetTree(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int depth = 3
    ) => TypedResults.Ok(await sender.Send(new GetBinaryTreeQuery(organizationId, agentId, depth)));

    public static async Task<NoContent> Approve(ISender sender, Guid organizationId, Guid agentId)
    {
        await sender.Send(new ApproveAgentCommand(organizationId, agentId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Reject(ISender sender, Guid organizationId, Guid agentId)
    {
        await sender.Send(new RejectAgentCommand(organizationId, agentId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Activate(ISender sender, Guid organizationId, Guid agentId)
    {
        await sender.Send(new ActivateAgentCommand(organizationId, agentId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Suspend(ISender sender, Guid organizationId, Guid agentId)
    {
        await sender.Send(new SuspendAgentCommand(organizationId, agentId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> Reactivate(
        ISender sender,
        Guid organizationId,
        Guid agentId
    )
    {
        await sender.Send(new ReactivateAgentCommand(organizationId, agentId));
        return TypedResults.NoContent();
    }

    public static async Task<Ok<IReadOnlyList<DownlineAgentDto>>> GetDownline(
        ISender sender,
        Guid organizationId,
        Guid agentId,
        int maxDepth = 10
    ) =>
        TypedResults.Ok(await sender.Send(new GetDownlineQuery(organizationId, agentId, maxDepth)));

    public static async Task<Ok<IReadOnlyList<PlacementChildDto>>> GetChildren(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) =>
        TypedResults.Ok(
            await sender.Send(new GetDirectPlacementChildrenQuery(organizationId, agentId))
        );

    public static async Task<Ok<IReadOnlyList<PlacementAncestorDto>>> GetAncestors(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) =>
        TypedResults.Ok(await sender.Send(new GetPlacementAncestorsQuery(organizationId, agentId)));

    public static async Task<Ok<IReadOnlyList<DirectRecruitDto>>> GetRecruits(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) => TypedResults.Ok(await sender.Send(new GetDirectRecruitsQuery(organizationId, agentId)));

    public static async Task<Ok<AgentLegSummaryDto>> GetLegSummary(
        ISender sender,
        Guid organizationId,
        Guid agentId
    ) => TypedResults.Ok(await sender.Send(new GetAgentLegSummaryQuery(organizationId, agentId)));
}

public sealed record ApplyAsAgentRequest(Guid? SponsorAgentId);

public sealed record PlaceAgentRequest(Guid ParentAgentId, PlacementSide Side);

public sealed record AutoPlaceAgentRequest(
    PlacementStrategyType? Strategy,
    PlacementSide? PreferredSide
);
