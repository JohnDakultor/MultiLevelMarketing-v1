using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;

public sealed class RetireCommissionPlanCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<RetireCommissionPlanCommand>
{
    public async Task Handle(
        RetireCommissionPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        var plan = await db.CommissionPlans.SingleOrDefaultAsync(
            x => x.Id == request.CommissionPlanId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (plan is null)
            throw new KeyNotFoundException("Commission plan was not found.");

        var beforeJson = Serialize(plan);
        plan.Retire(request.EffectiveTo);
        var audit = AuditCoverageMap.CommissionPlanRetired;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            plan.Id,
            beforeJson,
            Serialize(plan),
            request.Reason.Trim()
        );
        await db.SaveChangesAsync(cancellationToken);
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
            }
        );
}
