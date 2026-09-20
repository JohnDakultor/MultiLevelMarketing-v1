using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAdminAgents.Models;

namespace modular_mlm.Application.Network.Queries.GetAdminAgents;

public sealed class GetAdminAgentsQueryHandler(
    IApplicationDbContext db,
    IAgentIdentityReader identityReader
) : IRequestHandler<GetAdminAgentsQuery, AdminAgentsPageDto>
{
    public async Task<AdminAgentsPageDto> Handle(
        GetAdminAgentsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .Agents.AsNoTracking()
            .Where(agent => agent.OrganizationId == request.OrganizationId);
        if (request.Status.HasValue)
            query = query.Where(agent => agent.Status == request.Status);
        if (request.Placement == AgentPlacementFilter.Placed)
            query = query.Where(agent => agent.PlacementParentAgentId != null);
        if (request.Placement == AgentPlacementFilter.Unplaced)
            query = query.Where(agent => agent.PlacementParentAgentId == null);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(agent =>
                agent.AgentCode.Contains(search) || agent.ReferralCode.Contains(search)
            );
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var agents = await query
            .OrderByDescending(agent => agent.JoinedAt)
            .ThenBy(agent => agent.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var relatedIds = agents
            .SelectMany(agent => new[] { agent.SponsorAgentId, agent.PlacementParentAgentId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var codes = await db
            .Agents.AsNoTracking()
            .Where(agent =>
                agent.OrganizationId == request.OrganizationId && relatedIds.Contains(agent.Id)
            )
            .ToDictionaryAsync(agent => agent.Id, agent => agent.AgentCode, cancellationToken);
        var identities = await identityReader.GetByUserIdsAsync(
            agents.Select(agent => agent.UserId).Distinct().ToArray(),
            cancellationToken
        );
        var items = agents
            .Select(agent =>
            {
                identities.TryGetValue(agent.UserId, out var identity);
                return new AdminAgentSummaryDto(
                    agent.Id,
                    agent.AgentCode,
                    identity?.DisplayName ?? agent.AgentCode,
                    identity?.Email ?? string.Empty,
                    agent.Status,
                    agent.SponsorAgentId,
                    agent.SponsorAgentId.HasValue
                    && codes.TryGetValue(agent.SponsorAgentId.Value, out var sponsorCode)
                        ? sponsorCode
                        : null,
                    agent.JoinedAt,
                    agent.ActivatedAt,
                    agent.PlacementParentAgentId.HasValue,
                    agent.PlacementParentAgentId,
                    agent.PlacementSide,
                    agent.QualificationState
                );
            })
            .ToArray();
        return new AdminAgentsPageDto(items, request.Page, request.PageSize, totalCount);
    }
}
