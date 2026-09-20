using modular_mlm.Application.Network.Queries.GetDownline.Models;

namespace modular_mlm.Application.Network.Queries.GetDownline;

public sealed record GetDownlineQuery(Guid OrganizationId, Guid AgentId, int MaxDepth = 10)
    : IRequest<IReadOnlyList<DownlineAgentDto>>,
        IAgentScopedRequest;
