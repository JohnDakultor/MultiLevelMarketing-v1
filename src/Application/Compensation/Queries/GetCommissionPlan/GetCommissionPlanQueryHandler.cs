using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlan;

public sealed class GetCommissionPlanQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCommissionPlanQuery, CommissionPlanDto?>
{
    public Task<CommissionPlanDto?> Handle(
        GetCommissionPlanQuery request,
        CancellationToken cancellationToken
    ) =>
        db.CommissionPlans
            .AsNoTracking()
            .Where(plan =>
                plan.OrganizationId == request.OrganizationId
                && plan.Id == request.CommissionPlanId
            )
            .Select(plan => new CommissionPlanDto(
                plan.Id,
                plan.Name,
                plan.Version,
                plan.Status,
                plan.EffectiveFrom,
                plan.EffectiveTo,
                plan.DirectSalesRate,
                plan.BinaryPairing.Enabled,
                plan.BinaryPairing.PairingRate,
                plan.BinaryPairing.ProcessingFrequency,
                plan.BinaryPairing.CalculationType,
                plan.BinaryPairing.PairUnitBv,
                plan.BinaryPairing.FixedPairAmount,
                plan.BinaryPairing.CarryForwardEnabled,
                plan.QualificationRulesJson,
                plan.CapRulesJson,
                plan.ConfigurationVersion
            ))
            .SingleOrDefaultAsync(cancellationToken);
}
