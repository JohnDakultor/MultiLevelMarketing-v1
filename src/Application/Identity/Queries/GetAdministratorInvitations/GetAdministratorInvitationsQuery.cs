using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Identity.Queries.GetAdministratorInvitations.Models;
using modular_mlm.Domain.Constants;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Identity.Queries.GetAdministratorInvitations;

[Authorize(Roles = Roles.Administrator)]
public sealed record GetAdministratorInvitationsQuery(
    Guid OrganizationId,
    AdministratorInvitationStatus Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<AdministratorInvitationDto>>, IOrganizationAdminRequest;
