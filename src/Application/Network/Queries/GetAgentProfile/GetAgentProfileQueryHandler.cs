using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAgentProfile.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentProfile;

public sealed class GetAgentProfileQueryHandler(
    IApplicationDbContext db,
    IAgentIdentityReader identityReader
) : IRequestHandler<GetAgentProfileQuery, AgentProfileDto?>
{
    public async Task<AgentProfileDto?> Handle(
        GetAgentProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        var data = await (
            from agent in db.Agents.AsNoTracking()
            join sponsor in db.Agents.AsNoTracking()
                on agent.SponsorAgentId equals sponsor.Id
                into sponsors
            from sponsor in sponsors.DefaultIfEmpty()
            join parent in db.Agents.AsNoTracking()
                on agent.PlacementParentAgentId equals parent.Id
                into parents
            from parent in parents.DefaultIfEmpty()
            where agent.OrganizationId == request.OrganizationId && agent.Id == request.AgentId
            select new
            {
                Agent = agent,
                SponsorCode = sponsor == null ? null : sponsor.AgentCode,
                ParentCode = parent == null ? null : parent.AgentCode,
            }
        ).SingleOrDefaultAsync(cancellationToken);
        if (data is null)
            return null;

        var identities = await identityReader.GetByUserIdsAsync(
            [data.Agent.UserId],
            cancellationToken
        );
        identities.TryGetValue(data.Agent.UserId, out var identity);
        return new AgentProfileDto(
            data.Agent.Id,
            data.Agent.AgentCode,
            data.Agent.ReferralCode,
            identity?.DisplayName ?? data.Agent.AgentCode,
            identity?.Email ?? string.Empty,
            data.Agent.Status,
            data.Agent.SponsorAgentId,
            data.SponsorCode,
            data.Agent.PlacementParentAgentId,
            data.ParentCode,
            data.Agent.PlacementSide,
            data.Agent.PreferredLeg,
            data.Agent.JoinedAt,
            data.Agent.ActivatedAt,
            data.Agent.QualificationState
        );
    }
}
