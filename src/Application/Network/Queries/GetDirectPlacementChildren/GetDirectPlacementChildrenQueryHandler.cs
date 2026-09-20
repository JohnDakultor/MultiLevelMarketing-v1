using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetDirectPlacementChildren.Models;

namespace modular_mlm.Application.Network.Queries.GetDirectPlacementChildren;

public sealed class GetDirectPlacementChildrenQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectPlacementChildrenQuery, IReadOnlyList<PlacementChildDto>>
{
    public async Task<IReadOnlyList<PlacementChildDto>> Handle(
        GetDirectPlacementChildrenQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .Agents.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId
                && x.PlacementParentAgentId == request.AgentId
            )
            .OrderBy(x => x.PlacementSide)
            .Select(x => new PlacementChildDto(
                x.Id,
                x.AgentCode,
                x.Status,
                x.PlacementSide!.Value,
                x.JoinedAt
            ))
            .ToListAsync(cancellationToken);
}
