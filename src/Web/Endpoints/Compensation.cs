using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;
using modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;
using modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlans;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Web.Endpoints;

public sealed class Compensation : IEndpointGroup
{
    public static string? RoutePrefix =>
        "/api/organizations/{organizationId:guid}/admin/compensation";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetPlans, "plans");
        group.MapPost(CreatePlan, "plans");
        group.MapPost(PublishPlan, "plans/{commissionPlanId:guid}/publish");
        group.MapPost(RetirePlan, "plans/{commissionPlanId:guid}/retire");
    }

    public static async Task<Ok<IReadOnlyList<CommissionPlanDto>>> GetPlans(
        ISender sender,
        Guid organizationId
    ) => TypedResults.Ok(await sender.Send(new GetCommissionPlansQuery(organizationId)));

    public static async Task<Created<Guid>> CreatePlan(
        ISender sender,
        Guid organizationId,
        CreateCommissionPlanRequest request
    )
    {
        var id = await sender.Send(
            new CreateCommissionPlanCommand(
                organizationId,
                request.Name,
                request.Version,
                request.EffectiveFrom,
                request.DirectSalesRate,
                request.BinaryPairingEnabled,
                request.BinaryPairingRate,
                request.ProcessingFrequency,
                request.PairingCalculationType,
                request.PairUnitBv,
                request.FixedPairAmount,
                request.CarryForwardEnabled,
                request.QualificationRulesJson,
                request.CapRulesJson
            )
        );
        return TypedResults.Created(
            $"/api/organizations/{organizationId}/admin/compensation/plans/{id}",
            id
        );
    }

    public static async Task<NoContent> PublishPlan(
        ISender sender,
        Guid organizationId,
        Guid commissionPlanId
    )
    {
        await sender.Send(new PublishCommissionPlanCommand(organizationId, commissionPlanId));
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> RetirePlan(
        ISender sender,
        Guid organizationId,
        Guid commissionPlanId,
        RetireCommissionPlanRequest request
    )
    {
        await sender.Send(
            new RetireCommissionPlanCommand(
                organizationId,
                commissionPlanId,
                request.EffectiveTo,
                request.Reason
            )
        );
        return TypedResults.NoContent();
    }
}

public sealed record RetireCommissionPlanRequest(DateTimeOffset EffectiveTo, string Reason);

public sealed record CreateCommissionPlanRequest(
    string Name,
    int Version,
    DateTimeOffset EffectiveFrom,
    decimal DirectSalesRate,
    bool BinaryPairingEnabled,
    decimal? BinaryPairingRate,
    ProcessingFrequency ProcessingFrequency,
    PairingCalculationType PairingCalculationType = PairingCalculationType.PercentageMatchedVolume,
    decimal? PairUnitBv = null,
    decimal? FixedPairAmount = null,
    bool CarryForwardEnabled = true,
    string? QualificationRulesJson = null,
    string? CapRulesJson = null
);
