using modular_mlm.Application.Network.Queries.GetAgentApplications.Models;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentApplications;

public sealed record GetAgentApplicationsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    AgentStatus? Status = null,
    string? Search = null
) : IRequest<AgentApplicationsPageDto>, IOrganizationAdminRequest;
