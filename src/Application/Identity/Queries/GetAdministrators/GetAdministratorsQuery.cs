using modular_mlm.Application.Common.Security;
using modular_mlm.Application.Identity.Queries.GetAdministrators.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Identity.Queries.GetAdministrators;

[Authorize(Roles = Roles.Administrator)]
public sealed record GetAdministratorsQuery(Guid OrganizationId, int Page = 1, int PageSize = 20)
    : IRequest<List<AdministratorDto>>,
        IOrganizationAdminRequest;
