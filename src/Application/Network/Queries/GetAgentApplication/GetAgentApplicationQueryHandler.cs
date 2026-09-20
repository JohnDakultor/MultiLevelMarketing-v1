using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAgentApplication.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentApplication;

public sealed class GetAgentApplicationQueryHandler(IApplicationDbContext db, IUser user)
    : IRequestHandler<GetAgentApplicationQuery, AgentApplicationDto?>
{
    public Task<AgentApplicationDto?> Handle(
        GetAgentApplicationQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.Id ?? throw new UnauthorizedAccessException();
        return (
            from agent in db.Agents.AsNoTracking()
            join sponsor in db.Agents.AsNoTracking()
                on agent.SponsorAgentId equals sponsor.Id
                into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            where agent.OrganizationId == request.OrganizationId && agent.UserId == userId
            select new AgentApplicationDto(
                agent.Id,
                agent.AgentCode,
                agent.Status,
                agent.SponsorAgentId,
                sponsor == null ? null : sponsor.AgentCode,
                agent.JoinedAt,
                agent.ActivatedAt,
                agent.QualificationState,
                agent.PlacementParentAgentId == null
            )
        ).SingleOrDefaultAsync(cancellationToken);
    }
}
