using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.RejectAgent;

public sealed class RejectAgentCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<RejectAgentCommand>
{
    public async Task Handle(RejectAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents.SingleOrDefaultAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent application was not found.");

        // Repeating a successfully completed rejection is an idempotent no-op.
        // Other invalid lifecycle transitions remain protected by the domain entity.
        if (agent.Status == AgentStatus.Closed)
            return;

        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        agent.RejectApplication();
        var audit = AuditCoverageMap.AgentRejected;
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
