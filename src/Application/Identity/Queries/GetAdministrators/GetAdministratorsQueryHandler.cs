using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Queries.GetAdministrators.Models;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Identity.Queries.GetAdministrators;

public sealed class GetAdministratorsQueryHandler(
    IUser currentUser,
    IAdministratorAccountService administratorAccountService
) : IRequestHandler<GetAdministratorsQuery, List<AdministratorDto>>
{
    public async Task<List<AdministratorDto>> Handle(
        GetAdministratorsQuery request,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = currentUser.Id ?? throw new UnauthorizedAccessException();
        var mayManageOrganization = await administratorAccountService.CanManageOrganizationAsync(
            currentUserId,
            request.OrganizationId,
            cancellationToken
        );

        if (!mayManageOrganization)
            throw new ForbiddenAccessException();

        var administrators = await administratorAccountService.GetAdministratorsAsync(
            request.OrganizationId,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        return administrators
            .Select(administrator => new AdministratorDto(
                administrator.UserId,
                administrator.OrganizationId,
                administrator.DisplayName,
                administrator.Email,
                administrator.EmailConfirmed,
                Roles.Administrator
            ))
            .ToList();
    }
}
