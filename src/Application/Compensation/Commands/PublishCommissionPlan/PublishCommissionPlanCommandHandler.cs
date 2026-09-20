using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;

public sealed class PublishCommissionPlanCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<PublishCommissionPlanCommand>
{
    public async Task Handle(
        PublishCommissionPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        var plan = await db.CommissionPlans.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.CommissionPlanId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (plan is null)
            throw new KeyNotFoundException("Commission plan was not found.");

        var currentPlans = await db
            .CommissionPlans.Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Status == CommissionPlanStatus.Active
                && candidate.EffectiveTo == null
                && candidate.Id != plan.Id
            )
            .ToListAsync(cancellationToken);
        foreach (var current in currentPlans)
        {
            if (current.EffectiveFrom >= plan.EffectiveFrom)
                throw new InvalidOperationException(
                    "The new plan must start after the currently active plan."
                );
            var currentBefore = Serialize(current);
            current.Retire(plan.EffectiveFrom);
            var retirementAudit = AuditCoverageMap.CommissionPlanRetired;
            auditWriter.Write(
                request.OrganizationId,
                retirementAudit.Action,
                retirementAudit.EntityType,
                current.Id,
                currentBefore,
                Serialize(current),
                $"Superseded by commission plan {plan.Id}."
            );
        }

        var beforeJson = Serialize(plan);
        plan.Publish();
        var audit = AuditCoverageMap.CommissionPlanPublished;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            plan.Id,
            beforeJson,
            Serialize(plan),
            null
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
