using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;

namespace modular_mlm.Application.Network.Commands.SetPreferredLeg;

public sealed class SetPreferredLegCommandHandler(
    IApplicationDbContext db,
    IUser user,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<SetPreferredLegCommand>
{
    public async Task Handle(SetPreferredLegCommand request, CancellationToken cancellationToken)
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        var organization =
            await db
                .Organizations.AsNoTracking()
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.OrganizationId,
                    cancellationToken
                )
            ?? throw new KeyNotFoundException("Organization was not found.");
        if (
            !organization.Features.AgentProgramEnabled
            || !organization.Network.AllowAgentPreferredLeg
        )
            throw new InvalidOperationException("Preferred-leg selection is disabled.");
        var agent =
            await db.Agents.SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.UserId == userId,
                cancellationToken
            ) ?? throw new KeyNotFoundException("Current Agent profile was not found.");
        var before = AgentAuditSnapshot.Serialize(agent);
        if (!agent.SetPreferredLeg(request.PreferredLeg, timeProvider.GetUtcNow()))
            return;
        var audit = AuditCoverageMap.AgentPreferredLegChanged;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            agent.Id,
            before,
            AgentAuditSnapshot.Serialize(agent),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
