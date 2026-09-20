using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Network.Queries.GetCurrentAgentContext.Models;

namespace modular_mlm.Application.Network.Queries.GetCurrentAgentContext;

[Authorize]
public sealed record GetCurrentAgentContextQuery(Guid OrganizationId)
    : IRequest<CurrentAgentContextDto?>,
        ICurrentAgentRequest;
