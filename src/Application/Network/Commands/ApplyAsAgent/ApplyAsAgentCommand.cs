using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Network.Commands.ApplyAsAgent;

[Authorize]
public sealed record ApplyAsAgentCommand(Guid OrganizationId, Guid? SponsorAgentId)
    : IRequest<Guid>;
