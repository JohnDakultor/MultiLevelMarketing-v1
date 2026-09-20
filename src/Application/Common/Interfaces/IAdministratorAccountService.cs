using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IAdministratorAccountService
{
    Task<bool> CanManageOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<AdministratorAccountSummary>> GetAdministratorsAsync(
        Guid organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<bool> AdministratorExistsAsync(
        Guid organizationId,
        string email,
        CancellationToken cancellationToken
    );

    Task<Guid> CreateAdministratorAsync(
        Guid organizationId,
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken
    );

    Task StageAdministratorRoleRevocationAsync(
        Guid organizationId,
        Guid administratorUserId,
        CancellationToken cancellationToken
    );

    string NormalizeEmail(string email);
}
