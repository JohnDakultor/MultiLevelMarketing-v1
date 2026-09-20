using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAgentLegSummary.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentLegSummary;

public sealed class GetAgentLegSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentLegSummaryQuery, AgentLegSummaryDto>
{
    public async Task<AgentLegSummaryDto> Handle(
        GetAgentLegSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var exists = await db.Agents.AnyAsync(
            x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (!exists)
            throw new KeyNotFoundException("Agent was not found.");

        var legCounts = await db
            .PlacementClosures.AsNoTracking()
            .Where(x =>
                x.OrganizationId == request.OrganizationId && x.AncestorAgentId == request.AgentId
            )
            .GroupBy(x => x.FirstLeg)
            .Select(x => new { Side = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Side, x => x.Count, cancellationToken);
        var volume = await db
            .BinaryVolumeBalances.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == request.OrganizationId && x.AgentId == request.AgentId,
                cancellationToken
            );

        return new AgentLegSummaryDto(
            request.AgentId,
            legCounts.GetValueOrDefault(PlacementSide.Left),
            legCounts.GetValueOrDefault(PlacementSide.Right),
            volume?.LeftAvailable ?? 0,
            volume?.RightAvailable ?? 0,
            volume?.LeftLifetime ?? 0,
            volume?.RightLifetime ?? 0
        );
    }
}
