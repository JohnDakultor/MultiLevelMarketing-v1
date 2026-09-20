using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Identity.Commands.InviteAdministrator;

[Authorize(Roles = Roles.Administrator)]
public sealed record InviteAdministratorCommand(Guid OrganizationId, string Email)
    : IRequest<Guid>,
        IOrganizationAdminRequest;
