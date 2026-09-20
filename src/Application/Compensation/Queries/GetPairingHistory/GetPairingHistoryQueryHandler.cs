using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Queries.GetPairingHistory.Models;

namespace modular_mlm.Application.Compensation.Queries.GetPairingHistory;

public sealed class GetPairingHistoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPairingHistoryQuery, List<BinaryPairingRunDto>>
{
    public async Task<List<BinaryPairingRunDto>> Handle(
        GetPairingHistoryQuery request,
        CancellationToken cancellationToken
    )
    {
        if (request.OrganizationId == Guid.Empty)
            throw new ArgumentException(
                "Organization is required.",
                nameof(request.OrganizationId)
            );
        if (request.AgentId == Guid.Empty)
            throw new ArgumentException("Agent is required.", nameof(request.AgentId));
        if (request.PeriodStart >= request.PeriodEnd)
            throw new ArgumentException("Period start must be before period end.");

        var agentExists = await db
            .Agents.AsNoTracking()
            .AnyAsync(
                agent =>
                    agent.Id == request.AgentId && agent.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (!agentExists)
            throw new KeyNotFoundException("Agent was not found in this organization.");

        return await db
            .BinaryPairingRuns.AsNoTracking()
            .Where(run =>
                run.OrganizationId == request.OrganizationId
                && run.AgentId == request.AgentId
                && run.PeriodEnd > request.PeriodStart
                && run.PeriodStart < request.PeriodEnd
            )
            .OrderByDescending(run => run.PeriodEnd)
            .ThenByDescending(run => run.ProcessedAt)
            .Select(run => new BinaryPairingRunDto(
                run.OrganizationId,
                run.AgentId,
                run.CommissionPlanId,
                run.CommissionPlanVersion,
                run.PeriodStart,
                run.PeriodEnd,
                run.QualificationPassed,
                run.QualificationFailureReason,
                run.LeftBefore,
                run.RightBefore,
                run.MatchedVolume,
                run.LeftConsumed,
                run.RightConsumed,
                run.LeftAfter,
                run.RightAfter,
                run.GrossCommission,
                run.CappedAmount,
                run.NetCommission,
                run.CapApplied,
                run.Status,
                run.ProcessedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
