using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;

namespace modular_mlm.Application.Network.Commands.SuspendAgent;

public sealed class SuspendAgentCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<SuspendAgentCommand>
{
    public async Task Handle(SuspendAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents.SingleOrDefaultAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found.");

        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        agent.Suspend();
        var audit = AuditCoverageMap.AgentSuspended;
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
