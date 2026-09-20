using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;

public sealed class CreateCommissionPlanCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<CreateCommissionPlanCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCommissionPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        if (
            !await db
                .Organizations.AsNoTracking()
                .AnyAsync(
                    organization => organization.Id == request.OrganizationId,
                    cancellationToken
                )
        )
            throw new KeyNotFoundException("Organization was not found.");

        var duplicate = await db
            .CommissionPlans.AsNoTracking()
            .AnyAsync(
                plan =>
                    plan.OrganizationId == request.OrganizationId
                    && plan.Name == request.Name
                    && plan.Version == request.Version,
                cancellationToken
            );
        if (duplicate)
            throw new InvalidOperationException("The commission plan version already exists.");

        var plan = CommissionPlan.Draft(
            request.OrganizationId,
            request.Name,
            request.Version,
            request.EffectiveFrom
        );
        plan.ConfigureDirectSales(request.DirectSalesRate);
        if (request.BinaryPairingEnabled)
        {
            var pairingRule = request.PairingCalculationType switch
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
            plan.ConfigureBinaryPairing(pairingRule);
        }
        if (!string.IsNullOrWhiteSpace(request.QualificationRulesJson))
            plan.ConfigureQualificationRules(request.QualificationRulesJson);
        if (!string.IsNullOrWhiteSpace(request.CapRulesJson))
            plan.ConfigureCapRules(request.CapRulesJson);

        db.CommissionPlans.Add(plan);
        var audit = AuditCoverageMap.CommissionPlanCreated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            plan.Id,
            null,
            Serialize(plan),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }

    private static string Serialize(CommissionPlan plan) =>
        AuditJson.Serialize(
            new
            {
                plan.Name,
                plan.Version,
                plan.Status,
                plan.EffectiveFrom,
                plan.EffectiveTo,
                plan.DirectSalesRate,
                BinaryPairingEnabled = plan.BinaryPairing.Enabled,
            }
        );
}
