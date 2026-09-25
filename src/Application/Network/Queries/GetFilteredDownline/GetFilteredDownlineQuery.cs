using modular_mlm.Application.Network.Queries.GetFilteredDownline.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetFilteredDownline;

public sealed record GetFilteredDownlineQuery(
    Guid OrganizationId,
    Guid AgentId,
    int Page,
    int PageSize,
    int? MaxDepth,
    string? Search,
    PlacementSide? FirstLeg,
    bool DirectRecruitOnly,
    AgentStatus? Status
) : IRequest<FilteredDownlinePageDto>, IAgentScopedRequest;
