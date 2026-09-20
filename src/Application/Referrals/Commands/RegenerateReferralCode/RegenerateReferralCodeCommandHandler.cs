using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;

namespace modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;

public sealed class RegenerateReferralCodeCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<RegenerateReferralCodeCommand, string>
{
    public async Task<string> Handle(
        RegenerateReferralCodeCommand request,
        CancellationToken cancellationToken
    )
    {
        var agent = await db.Agents.SingleOrDefaultAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (agent is null)
            throw new KeyNotFoundException("Agent was not found.");

        var beforeJson = AgentAuditSnapshot.Serialize(agent);
        string code;
        do
        {
            code = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        } while (
            await db.Agents.AnyAsync(
                x => x.OrganizationId == request.OrganizationId && x.ReferralCode == code,
                cancellationToken
            )
        );

        agent.RegenerateReferralCode(code);
        var audit = AuditCoverageMap.ReferralCodeRegenerated;
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
        return code;
    }
}
