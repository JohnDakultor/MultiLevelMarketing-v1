using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Wallets;

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

        var accessValidation = await identityService.ValidateAgentAccessAssignmentAsync(
            agent.UserId,
            request.OrganizationId,
            cancellationToken
        );
        if (!accessValidation.Succeeded)
            throw new ConflictException(string.Join("; ", accessValidation.Errors));

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
        if (
            !await db.AgentWallets.AnyAsync(
                wallet =>
                    wallet.OrganizationId == request.OrganizationId
                    && wallet.AgentId == agent.Id,
                cancellationToken
            )
        )
        {
            var currency = await db.Organizations
                .Where(organization => organization.Id == request.OrganizationId)
                .Select(organization => organization.CurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null)
                throw new KeyNotFoundException("Organization was not found.");
            db.AgentWallets.Add(AgentWallet.Open(request.OrganizationId, agent.Id, currency));
        }
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
