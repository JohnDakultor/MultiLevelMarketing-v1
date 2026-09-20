using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetBinaryTree.Models;

namespace modular_mlm.Application.Network.Queries.GetBinaryTree;

public sealed class GetBinaryTreeQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBinaryTreeQuery, IReadOnlyList<BinaryTreeNodeDto>>
{
    public async Task<IReadOnlyList<BinaryTreeNodeDto>> Handle(
        GetBinaryTreeQuery request,
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
        var maxDepth = Math.Min(request.Depth, configuredDepth);
        return await (
            from closure in db.PlacementClosures.AsNoTracking()
            join agent in db.Agents.AsNoTracking() on closure.DescendantAgentId equals agent.Id
            where
                closure.OrganizationId == request.OrganizationId
                && closure.AncestorAgentId == request.AgentId
                && closure.Depth <= maxDepth
            orderby closure.Depth, closure.FirstLeg, agent.JoinedAt
            select new BinaryTreeNodeDto(
                agent.Id,
                agent.AgentCode,
                agent.ReferralCode,
                agent.Status,
                agent.PlacementParentAgentId,
                agent.PlacementSide,
                closure.Depth
            )
        ).ToListAsync(cancellationToken);
    }
}
