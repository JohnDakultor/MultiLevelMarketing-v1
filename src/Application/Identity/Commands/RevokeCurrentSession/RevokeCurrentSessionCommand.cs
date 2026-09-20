using modular_mlm.Application.Common.Security;

namespace modular_mlm.Application.Identity.Commands.RevokeCurrentSession;

[Authorize]
public sealed record RevokeCurrentSessionCommand(Guid SessionId) : IRequest;
