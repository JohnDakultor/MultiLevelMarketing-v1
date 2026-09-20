using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAgentApplications.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentApplications;

public sealed class GetAgentApplicationsQueryHandler(
    IApplicationDbContext db,
    IAgentIdentityReader identityReader
) : IRequestHandler<GetAgentApplicationsQuery, AgentApplicationsPageDto>
{
    public async Task<AgentApplicationsPageDto> Handle(
        GetAgentApplicationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = db
            .Agents.AsNoTracking()
            .Where(agent =>
                agent.OrganizationId == request.OrganizationId
                && (
                    agent.Status == AgentStatus.Applied
                    || agent.Status == AgentStatus.PendingApproval
                    || agent.Status == AgentStatus.Closed
                )
            );
        if (request.Status.HasValue)
            query = query.Where(agent => agent.Status == request.Status);
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
        var sponsorIds = agents
            .Where(agent => agent.SponsorAgentId.HasValue)
            .Select(agent => agent.SponsorAgentId!.Value)
            .Distinct()
            .ToArray();
        var sponsorCodes = await db
            .Agents.AsNoTracking()
            .Where(agent =>
                agent.OrganizationId == request.OrganizationId && sponsorIds.Contains(agent.Id)
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
                return new AgentApplicationSummaryDto(
                    agent.Id,
                    identity?.DisplayName ?? agent.AgentCode,
                    identity?.Email ?? string.Empty,
                    agent.AgentCode,
                    agent.Status,
                    agent.SponsorAgentId,
                    agent.SponsorAgentId.HasValue
                    && sponsorCodes.TryGetValue(agent.SponsorAgentId.Value, out var code)
                        ? code
                        : null,
                    agent.JoinedAt,
                    agent.PlacementParentAgentId.HasValue
                );
            })
            .ToArray();
        return new AgentApplicationsPageDto(items, request.Page, request.PageSize, totalCount);
    }
}
