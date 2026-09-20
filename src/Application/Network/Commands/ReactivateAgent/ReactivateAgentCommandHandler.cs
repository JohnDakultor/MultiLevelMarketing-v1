using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;

namespace modular_mlm.Application.Network.Commands.ReactivateAgent;

public sealed class ReactivateAgentCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<ReactivateAgentCommand>
{
    public async Task Handle(ReactivateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents.SingleOrDefaultAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found.");

        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        agent.Reactivate();
        var audit = AuditCoverageMap.AgentReactivated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            agent.Id,
            beforeJson,
            AgentAuditSnapshot.Serialize(agent),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
