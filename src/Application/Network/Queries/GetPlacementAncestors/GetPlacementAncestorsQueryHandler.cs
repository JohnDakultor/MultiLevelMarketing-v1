using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetPlacementAncestors.Models;

namespace modular_mlm.Application.Network.Queries.GetPlacementAncestors;

public sealed class GetPlacementAncestorsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPlacementAncestorsQuery, IReadOnlyList<PlacementAncestorDto>>
{
    public async Task<IReadOnlyList<PlacementAncestorDto>> Handle(
        GetPlacementAncestorsQuery request,
        CancellationToken cancellationToken
    ) =>
        await (
            from closure in db.PlacementClosures.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on closure.AncestorAgentId equals agent.Id
            where
                closure.OrganizationId == request.OrganizationId
                && closure.DescendantAgentId == request.AgentId
            orderby closure.Depth
            select new PlacementAncestorDto(
                agent.Id,
                agent.AgentCode,
                agent.Status,
                closure.Depth,
                closure.FirstLeg
            )
        ).ToListAsync(cancellationToken);
}
