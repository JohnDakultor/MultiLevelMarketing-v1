using modular_mlm.Application.Network.Queries.GetFilteredDownline.Models;

namespace modular_mlm.Application.Network.Queries.GetFilteredDownline;

public sealed class GetFilteredDownlineQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetFilteredDownlineQuery, FilteredDownlinePageDto>
{
    public async Task<FilteredDownlinePageDto> Handle(
        GetFilteredDownlineQuery request,
        CancellationToken cancellationToken
    )
    {
        var configuredDepth = await (
            from organization in db.Organizations.AsNoTracking()
            join root in db.Agents.AsNoTracking()
                on organization.Id equals root.OrganizationId
            where
                organization.Id == request.OrganizationId
                && root.Id == request.AgentId
            select (int?)organization.Network.MaxQueryDepth
        ).SingleOrDefaultAsync(cancellationToken);
        if (!configuredDepth.HasValue)
            throw new KeyNotFoundException("Root agent was not found in this organization.");

        var maxDepth = Math.Min(
            request.MaxDepth ?? configuredDepth.Value,
            configuredDepth.Value
        );
        var query =
            from closure in db.PlacementClosures.AsNoTracking()
            join agent in db.Agents.AsNoTracking()
                on new { closure.DescendantAgentId, closure.OrganizationId } equals new
                {
                    DescendantAgentId = agent.Id,
                    agent.OrganizationId,
                }
            where
                closure.OrganizationId == request.OrganizationId
                && closure.AncestorAgentId == request.AgentId
                && closure.Depth >= 1
                && closure.Depth <= maxDepth
            select new { closure, agent };

        if (request.FirstLeg.HasValue)
            query = query.Where(row => row.closure.FirstLeg == request.FirstLeg.Value);
        if (request.Status.HasValue)
            query = query.Where(row => row.agent.Status == request.Status.Value);
        if (request.DirectRecruitOnly)
            query = query.Where(row => row.agent.SponsorAgentId == request.AgentId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalizedSearch = request.Search.Trim().ToUpperInvariant();
            query = query.Where(row => row.agent.AgentCode.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(row => row.closure.Depth)
            .ThenBy(row => row.closure.FirstLeg)
            .ThenBy(row => row.agent.JoinedAt)
            .ThenBy(row => row.agent.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(row => new FilteredDownlineAgentDto(
                row.agent.Id,
                row.agent.AgentCode,
                row.agent.Status,
                row.agent.SponsorAgentId,
                row.agent.PlacementParentAgentId,
                row.agent.PlacementSide,
                row.closure.Depth,
                row.closure.FirstLeg,
                row.agent.QualificationState,
                row.agent.JoinedAt,
                row.agent.ActivatedAt
            ))
            .ToListAsync(cancellationToken);

        return new FilteredDownlinePageDto(
            request.OrganizationId,
            request.AgentId,
            items,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}
