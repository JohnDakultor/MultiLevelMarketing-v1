using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Network.Commands.AutoPlaceAgent;

public sealed class AutoPlaceAgentCommandHandler(
    IApplicationDbContext db,
    IAgentPlacementRepository placementRepository,
    IAgentPlacementMutationRepository placementMutationRepository,
    PlacementStrategyResolver strategyResolver,
    IAuditWriter auditWriter
) : IRequestHandler<AutoPlaceAgentCommand, PlacementDecision>
{
    private const int MaximumPlacementAttempts = 3;

    public async Task<PlacementDecision> Handle(
        AutoPlaceAgentCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db
            .Organizations.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );
        if (organization is null)
            throw new InvalidOperationException("Organization was not found.");
        if (!organization.Network.AutoPlacementEnabled)
            throw new InvalidOperationException("Automatic placement is disabled.");
        if (request.PreferredSide.HasValue && !organization.Network.AllowAgentPreferredLeg)
            throw new InvalidOperationException("Preferred-leg placement is disabled.");

        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.AgentId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );

        if (agent is null)
            throw new InvalidOperationException("Agent was not found in this organization.");
        if (agent.PlacementParentAgentId is not null)
            throw new InvalidOperationException("Agent is already placed.");
        if (agent.SponsorAgentId is null)
            throw new InvalidOperationException(
                "Automatic placement requires a sponsor to define the placement subtree."
            );
        if (agent.Status is AgentStatus.Suspended or AgentStatus.Closed or AgentStatus.Inactive)
            throw new InvalidOperationException(
                "Agent is not eligible for automatic placement in the current state."
            );

        var beforeJson = AgentAuditSnapshot.Serialize(agent);

        var strategyType = request.Strategy ?? organization.Network.DefaultPlacementStrategy;
        if (strategyType == PlacementStrategyType.PreferredLeg && request.PreferredSide is null)
            throw new InvalidOperationException("The preferred-leg strategy requires a side.");
        var strategy = strategyResolver.Resolve(strategyType);

        for (var attempt = 0; attempt < MaximumPlacementAttempts; attempt++)
        {
            var candidates = await placementRepository.GetCandidatesAsync(
                request.OrganizationId,
                agent.SponsorAgentId.Value,
                organization.Network.MaxQueryDepth,
                cancellationToken
            );
            var decision = strategy.SelectSlot(candidates, request.PreferredSide);

            if (
                await placementMutationRepository.TryPlaceAsync(
                    request.OrganizationId,
                    request.AgentId,
                    decision,
                    placedAgent =>
                    {
                        var audit = AuditCoverageMap.AgentAutoPlaced;
                        auditWriter.Write(
                            request.OrganizationId,
                            audit.Action,
                            audit.EntityType,
                            placedAgent.Id,
                            beforeJson,
                            AgentAuditSnapshot.Serialize(placedAgent),
                            reason: null
                        );
                    },
                    cancellationToken
                )
            )
                return decision;
        }

        throw new PlacementConflictException();
    }
}
