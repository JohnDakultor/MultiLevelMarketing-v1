using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetDownline.Models;

namespace modular_mlm.Application.Network.Queries.GetDownline;

public sealed class GetDownlineQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDownlineQuery, IReadOnlyList<DownlineAgentDto>>
{
    public async Task<IReadOnlyList<DownlineAgentDto>> Handle(
        GetDownlineQuery request,
        CancellationToken cancellationToken
    )
    {
        var configuredDepth = await db
            .Organizations.AsNoTracking()
            .Where(x => x.Id == request.OrganizationId)
            .Select(x => x.Network.MaxQueryDepth)
            .SingleOrDefaultAsync(cancellationToken);
        if (configuredDepth == 0)
            throw new KeyNotFoundException("Organization was not found.");
        var maxDepth = Math.Min(request.MaxDepth, configuredDepth);

        return await (
            from closure in db.PlacementClosures.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on closure.DescendantAgentId equals agent.Id
            where
                closure.OrganizationId == request.OrganizationId
                && closure.AncestorAgentId == request.AgentId
                && closure.Depth <= maxDepth
            orderby closure.Depth, closure.FirstLeg, agent.JoinedAt, agent.Id
            select new DownlineAgentDto(
                agent.Id,
                agent.AgentCode,
                agent.Status,
                agent.PlacementParentAgentId,
                agent.PlacementSide,
                closure.Depth,
                closure.FirstLeg,
                agent.JoinedAt
            )
        ).ToListAsync(cancellationToken);
    }
}
