using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlans;

public sealed class GetCommissionPlansQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCommissionPlansQuery, IReadOnlyList<CommissionPlanDto>>
{
    public async Task<IReadOnlyList<CommissionPlanDto>> Handle(
        GetCommissionPlansQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .CommissionPlans.AsNoTracking()
            .Where(plan => plan.OrganizationId == request.OrganizationId)
            .OrderByDescending(plan => plan.Version)
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
            .ToListAsync(cancellationToken);
}
