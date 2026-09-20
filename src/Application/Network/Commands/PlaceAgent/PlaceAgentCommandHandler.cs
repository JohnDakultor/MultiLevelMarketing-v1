using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Network.Commands.PlaceAgent;

public sealed class PlaceAgentCommandHandler(
    IApplicationDbContext db,
    IAgentPlacementMutationRepository placementRepository,
    IAuditWriter auditWriter
) : IRequestHandler<PlaceAgentCommand>
{
    public async Task Handle(PlaceAgentCommand request, CancellationToken cancellationToken)
    {
        var organization = await db
            .Organizations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.OrganizationId, cancellationToken);
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");
        var agent = await db
            .Agents.AsNoTracking()
            .SingleAsync(
                x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (
            organization.Network.RestrictPlacementChangesAfterActivation
            && agent.Status == AgentStatus.Active
        )
            throw new InvalidOperationException(
                "Placement changes are restricted after agent activation."
            );
        var parentExists = await db.Agents.AnyAsync(
            x => x.Id == request.ParentAgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (!parentExists)
            throw new InvalidOperationException(
                "Placement parent was not found in this organization."
            );
        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var placed = await placementRepository.TryPlaceAsync(
                request.OrganizationId,
                request.AgentId,
                new PlacementDecision(request.ParentAgentId, request.Side),
                placedAgent =>
                {
                    var audit = AuditCoverageMap.AgentPlaced;
                    auditWriter.Write(
                        request.OrganizationId,
                        audit.Action,
                        audit.EntityType,
                        placedAgent.Id,
                        beforeJson,
                        AgentAuditSnapshot.Serialize(placedAgent),
                        null
                    );
                },
                cancellationToken
            );
            if (placed)
                return;
        }
        throw new PlacementConflictException();
    }
}
