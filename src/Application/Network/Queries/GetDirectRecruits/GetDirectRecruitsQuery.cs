using modular_mlm.Application.Network.Queries.GetDirectRecruits.Models;

namespace modular_mlm.Application.Network.Queries.GetDirectRecruits;

public sealed record GetDirectRecruitsQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<IReadOnlyList<DirectRecruitDto>>,
        IAgentScopedRequest;
