using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetCurrentAgentContext;

public sealed class GetCurrentAgentContextQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetCurrentAgentContextQuery, CurrentAgentContextDto?>
{
    public Task<CurrentAgentContextDto?> Handle(
        GetCurrentAgentContextQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        return (
            from agent in db.Agents.AsNoTracking()
            join organization in db.Organizations.AsNoTracking()
                on agent.OrganizationId equals organization.Id
            where agent.OrganizationId == request.OrganizationId && agent.UserId == userId
            select new CurrentAgentContextDto(
                agent.Id,
                agent.AgentCode,
                agent.Status,
                agent.PlacementParentAgentId != null,
                organization.Features.AgentProgramEnabled
                    && organization.Network.AllowAgentPreferredLeg
                    && agent.Status != AgentStatus.Suspended
                    && agent.Status != AgentStatus.Closed,
                organization.Features.AgentProgramEnabled && agent.Status == AgentStatus.Active,
                organization.Features.WalletEnabled
                    && organization.Features.PayoutEnabled
                    && agent.Status == AgentStatus.Active,
                agent.QualificationState
            )
        ).SingleOrDefaultAsync(cancellationToken);
    }
}
