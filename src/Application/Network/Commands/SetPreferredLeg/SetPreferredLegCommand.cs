using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Commands.SetPreferredLeg;

[Authorize]
public sealed record SetPreferredLegCommand(Guid OrganizationId, PlacementSide PreferredLeg)
    : IRequest,
        ICurrentAgentRequest;
