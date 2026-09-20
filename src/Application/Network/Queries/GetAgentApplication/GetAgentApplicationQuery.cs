using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Network.Queries.GetAgentApplication.Models;

namespace modular_mlm.Application.Network.Queries.GetAgentApplication;

[Authorize]
public sealed record GetAgentApplicationQuery(Guid OrganizationId)
    : IRequest<AgentApplicationDto?>;
