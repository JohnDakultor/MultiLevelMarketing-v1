using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext;
using modular_mlm.Application.Referrals.Commands.CreateProductReferralLink;
using modular_mlm.Application.Referrals.Commands.CreateProductReferralLink.Models;
using modular_mlm.Application.Referrals.Queries.GetReferralLink;
using modular_mlm.Application.Referrals.Queries.GetReferralLink.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Contracts.Agents;

namespace modular_mlm.Web.Endpoints;

public sealed class AgentReferralTools : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/agent/referral";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Agent));
        group.MapGet(GetCurrentReferralLink);
        group.MapPost(CreateProductLink, "product-links");
    }

    public static async Task<Results<Ok<ReferralLinkDto>, NotFound>> GetCurrentReferralLink(
        ISender sender,
        Guid organizationId
    )
    {
        var context = await sender.Send(new GetCurrentAgentContextQuery(organizationId));
        if (context is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(
            await sender.Send(new GetReferralLinkQuery(organizationId, context.AgentId))
        );
    }

    public static async Task<Ok<ProductReferralLinkDto>> CreateProductLink(
        ISender sender,
        Guid organizationId,
        CreateProductReferralLinkRequest request
    ) =>
        TypedResults.Ok(
            await sender.Send(
                new CreateProductReferralLinkCommand(organizationId, request.ProductId)
            )
        );
}
