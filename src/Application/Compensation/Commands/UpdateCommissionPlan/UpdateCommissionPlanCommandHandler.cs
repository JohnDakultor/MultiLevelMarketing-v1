using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.UpdateCommissionPlan;

public sealed class UpdateCommissionPlanCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateCommissionPlanCommand>
{
    public async Task Handle(UpdateCommissionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await db.CommissionPlans.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.CommissionPlanId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (plan is null)
            throw new KeyNotFoundException("Commission plan was not found.");
        if (plan.ConfigurationVersion != request.ExpectedConfigurationVersion)
            throw new DatabaseConcurrencyConflictException(
                "commission plan",
                plan.Id.ToString()
            );

        var beforeJson = Serialize(plan);
        plan.AdvanceConfigurationVersion();
        plan.ConfigureDirectSales(request.DirectSalesEnabled ? request.DirectSalesRate : 0m);
        plan.ConfigureBinaryPairing(CreatePairingRule(request));
        plan.ConfigureQualificationRules(request.QualificationRulesJson);
        plan.ConfigureCapRules(request.CapRulesJson);

        var audit = AuditCoverageMap.CommissionPlanUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            plan.Id,
            beforeJson,
            Serialize(plan),
            reason: null
        );

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new DatabaseConcurrencyConflictException(
                "commission plan",
                plan.Id.ToString()
            );
        }
    }

    private static BinaryPairingRule CreatePairingRule(UpdateCommissionPlanCommand request)
    {
        if (!request.BinaryPairingEnabled)
            return BinaryPairingRule.Disabled();

        return request.PairingCalculationType switch
        {
            PairingCalculationType.PercentageMatchedVolume => BinaryPairingRule.Percentage(
                request.BinaryPairingRate!.Value,
                request.ProcessingFrequency,
                request.CarryForwardEnabled
            ),
            PairingCalculationType.FixedPerPair => BinaryPairingRule.FixedPerPair(
                request.PairUnitBv!.Value,
                request.FixedPairAmount!.Value,
                request.ProcessingFrequency,
                request.CarryForwardEnabled
            ),
            _ => throw new InvalidOperationException("Unsupported pairing calculation type."),
        };
    }

    private static string Serialize(CommissionPlan plan) =>
        AuditJson.Serialize(
            new
            {
                plan.Name,
                plan.Version,
                plan.ConfigurationVersion,
                plan.Status,
                plan.DirectSalesRate,
                BinaryPairing = new
                {
                    plan.BinaryPairing.Enabled,
                    plan.BinaryPairing.CalculationType,
                    plan.BinaryPairing.PairingRate,
                    plan.BinaryPairing.PairUnitBv,
                    plan.BinaryPairing.FixedPairAmount,
                    plan.BinaryPairing.ProcessingFrequency,
                    plan.BinaryPairing.CarryForwardEnabled,
                },
                plan.QualificationRulesJson,
                plan.CapRulesJson,
            }
        );
}
