using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Identity.Queries.GetAuthenticationSessions;

public sealed record GetAuthenticationSessionsQuery
    : IRequest<IReadOnlyList<AuthenticationSessionInfo>>;
