using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Identity.Queries.GetCurrentUser.Models;

namespace modular_mlm.Application.Identity.Queries.GetCurrentUser;

[Authorize]
public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;
