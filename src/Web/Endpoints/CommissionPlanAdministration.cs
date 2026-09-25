using Microsoft.AspNetCore.Http.HttpResults;
using modular_mlm.Application.Compensation.Commands.UpdateCommissionPlan;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlan;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Constants;
using modular_mlm.Web.Infrastructure.Security;

namespace modular_mlm.Web.Endpoints;

public sealed class CommissionPlanAdministration : IEndpointGroup
{
    public static string RoutePrefix => "/api/organizations/{organizationId:guid}/admin/compensation";

    public static void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        group.MapGet(GetPlan, "plans/{commissionPlanId:guid}");
        group.MapPut(UpdatePlan, "plans/{commissionPlanId:guid}")
            .RequireRateLimiting(RateLimitPolicyNames.AdministratorFinancialAction);
    }

    public static async Task<Results<Ok<CommissionPlanDto>, NotFound>> GetPlan(
        ISender sender,
        Guid organizationId,
        Guid commissionPlanId
    )
    {
        var result = await sender.Send(new GetCommissionPlanQuery(organizationId, commissionPlanId));
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    public static async Task<NoContent> UpdatePlan(
        ISender sender,
        Guid organizationId,
        Guid commissionPlanId,
        UpdateCommissionPlanRequest request
    )
    {
        await sender.Send(new UpdateCommissionPlanCommand(
            organizationId,
            commissionPlanId,
            request.DirectSalesEnabled,
            request.DirectSalesRate,
            request.BinaryPairingEnabled,
            request.PairingCalculationType,
            request.BinaryPairingRate,
            request.PairUnitBv,
            request.FixedPairAmount,
            request.ProcessingFrequency,
            request.CarryForwardEnabled,
            request.QualificationRulesJson,
            request.CapRulesJson,
            request.ExpectedConfigurationVersion
        ));
        return TypedResults.NoContent();
    }
}

public sealed record UpdateCommissionPlanRequest(
    bool DirectSalesEnabled,
    decimal DirectSalesRate,
    bool BinaryPairingEnabled,
    PairingCalculationType PairingCalculationType,
    decimal? BinaryPairingRate,
    decimal? PairUnitBv,
    decimal? FixedPairAmount,
    ProcessingFrequency ProcessingFrequency,
    bool CarryForwardEnabled,
    string QualificationRulesJson,
    string CapRulesJson,
    long ExpectedConfigurationVersion
);
