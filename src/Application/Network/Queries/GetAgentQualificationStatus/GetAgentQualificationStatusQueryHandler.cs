using System.Text.Json;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAgentQualificationStatus.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Network.Queries.GetAgentQualificationStatus;

public sealed class GetAgentQualificationStatusQueryHandler(
    IApplicationDbContext db,
    TimeProvider timeProvider
) : IRequestHandler<GetAgentQualificationStatusQuery, AgentQualificationStatusDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AgentQualificationStatusDto> Handle(
        GetAgentQualificationStatusQuery request,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow();
        var agent =
            await db
                .Agents.AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate =>
                        candidate.OrganizationId == request.OrganizationId
                        && candidate.Id == request.AgentId,
                    cancellationToken
                )
            ?? throw new KeyNotFoundException("Agent was not found.");
        var plan = await db
            .CommissionPlans.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Status == CommissionPlanStatus.Active
                && candidate.EffectiveFrom <= now
                && (candidate.EffectiveTo == null || candidate.EffectiveTo > now)
            )
            .OrderByDescending(candidate => candidate.Version)
            .FirstOrDefaultAsync(cancellationToken);
        var rules = plan is null
            ? QualificationRules.None()
            : ReadRules(plan.QualificationRulesJson);
        var periodStart = plan?.EffectiveFrom ?? DateTimeOffset.MinValue;
        var paidItems =
            from item in db.OrderItems.AsNoTracking()
            join order in db.Orders.AsNoTracking() on item.OrderId equals order.Id
            where
                order.OrganizationId == request.OrganizationId
                && order.AttributedAgentId == request.AgentId
                && order.PaymentStatus == PaymentStatus.Paid
                && order.PaidAt >= periodStart
                && order.PaidAt <= now
            select item;
        var sales =
            await paidItems.SumAsync(item => (decimal?)item.CommissionableAmount, cancellationToken)
            ?? 0m;
        var bv =
            await paidItems.SumAsync(item => (decimal?)item.BusinessVolume, cancellationToken)
            ?? 0m;
        var directs = await db.Agents.CountAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.SponsorAgentId == request.AgentId
                && candidate.Status == AgentStatus.Active,
            cancellationToken
        );
        var activeLegs = await db
            .Agents.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.PlacementParentAgentId == request.AgentId
                && candidate.Status == AgentStatus.Active
            )
            .Select(candidate => candidate.PlacementSide)
            .ToListAsync(cancellationToken);
        var input = new QualificationInput(
            agent.Status,
            agent.QualificationState,
            sales,
            bv,
            directs,
            activeLegs.Contains(PlacementSide.Left),
            activeLegs.Contains(PlacementSide.Right)
        );
        var result = new QualificationEvaluator().Evaluate(input, rules);
        return new AgentQualificationStatusDto(
            result.Passed,
            agent.QualificationState,
            result
                .Failures.Select(failure => new AgentQualificationFailureDto(
                    failure.Code,
                    failure.Message
                ))
                .ToArray(),
            sales,
            bv,
            directs,
            input.HasActiveLeftLeg,
            input.HasActiveRightLeg,
            plan?.Id,
            plan?.Version,
            plan?.EffectiveFrom,
            now
        );
    }

    private static QualificationRules ReadRules(string json) =>
        string.IsNullOrWhiteSpace(json) || json.Trim() is "[]" or "{}"
            ? QualificationRules.None()
            : JsonSerializer.Deserialize<QualificationRules>(json, JsonOptions)
                ?? throw new InvalidOperationException("Qualification rules JSON is invalid.");
}
