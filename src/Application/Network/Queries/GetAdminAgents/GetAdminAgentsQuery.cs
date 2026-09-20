using modular_mlm.Application.Network.Queries.GetAdminAgents.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAdminAgents;

public enum AgentPlacementFilter
{
    All,
    Placed,
    Unplaced,
}

public sealed record GetAdminAgentsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    AgentStatus? Status = null,
    AgentPlacementFilter Placement = AgentPlacementFilter.All,
    string? Search = null
) : IRequest<AdminAgentsPageDto>, IOrganizationAdminRequest;
