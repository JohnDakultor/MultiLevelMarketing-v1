using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Network.Commands.SetPreferredLeg;
using modular_mlm.Application.Network.Queries.GetAgentApplication;
using modular_mlm.Application.Network.Queries.GetAgentApplication.Models;
using modular_mlm.Application.Network.Queries.GetAgentProfile;
using modular_mlm.Application.Network.Queries.GetAgentProfile.Models;
using modular_mlm.Application.Network.Queries.GetAgentQualificationStatus;
using modular_mlm.Application.Network.Queries.GetAgentQualificationStatus.Models;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Contracts.Agents;

namespace modular_mlm.Web.Endpoints;

public sealed class CurrentAgent : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/agent";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
        group.MapGet(GetContext, "context");
        group.MapGet(GetApplication, "application");
        group
            .MapGet(GetProfile, "profile")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
        group
            .MapGet(GetQualification, "qualification")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
        group
            .MapPut(SetPreferredLeg, "preferred-leg")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
    }

    public static async Task<Results<Ok<CurrentAgentContextDto>, NotFound>> GetContext(
        ISender sender,
        Guid organizationId
    )
    {
        var context = await sender.Send(new GetCurrentAgentContextQuery(organizationId));
        return context is null ? TypedResults.NotFound() : TypedResults.Ok(context);
    }

    public static async Task<Results<Ok<AgentApplicationDto>, NotFound>> GetApplication(
        ISender sender,
        Guid organizationId
    )
    {
        var application = await sender.Send(new GetAgentApplicationQuery(organizationId));
        return application is null ? TypedResults.NotFound() : TypedResults.Ok(application);
    }

    public static async Task<Results<Ok<AgentProfileDto>, NotFound>> GetProfile(
        ISender sender,
        Guid organizationId
    )
    {
        var context = await sender.Send(new GetCurrentAgentContextQuery(organizationId));
        if (context is null)
            return TypedResults.NotFound();
        var profile = await sender.Send(new GetAgentProfileQuery(organizationId, context.AgentId));
        return profile is null ? TypedResults.NotFound() : TypedResults.Ok(profile);
    }

    public static async Task<Results<Ok<AgentQualificationStatusDto>, NotFound>> GetQualification(
        ISender sender,
        Guid organizationId
    )
    {
        var context = await sender.Send(new GetCurrentAgentContextQuery(organizationId));
        if (context is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(
            await sender.Send(new GetAgentQualificationStatusQuery(organizationId, context.AgentId))
        );
    }

    public static async Task<NoContent> SetPreferredLeg(
        ISender sender,
        Guid organizationId,
        SetPreferredLegRequest request
    )
    {
        await sender.Send(new SetPreferredLegCommand(organizationId, request.PreferredLeg));
        return TypedResults.NoContent();
    }
}
