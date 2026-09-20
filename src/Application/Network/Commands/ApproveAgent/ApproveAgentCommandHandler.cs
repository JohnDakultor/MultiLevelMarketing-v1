using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.ApproveAgent;

public sealed class ApproveAgentCommandHandler(
    IApplicationDbContext db,
    IIdentityService identityService,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<ApproveAgentCommand>
{
    public async Task Handle(ApproveAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.Agents.SingleOrDefaultAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent application was not found.");
        if (agent.Status != AgentStatus.PendingApproval)
            throw new InvalidOperationException("Only a pending application can be approved.");

        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        agent.Activate(clock.GetUtcNow());
        var audit = AuditCoverageMap.AgentApproved;
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

        var accessResult = await identityService.GrantAgentAccessAsync(
            agent.UserId,
            request.OrganizationId,
            cancellationToken
        );
        if (!accessResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", accessResult.Errors));
    }
}
