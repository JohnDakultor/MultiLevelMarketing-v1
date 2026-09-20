using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface INotificationRecipientResolver
{
    Task<NotificationRecipient?> FindAsync(
        Guid organizationId,
        string userId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<NotificationRecipient>> GetOrganizationAdministratorsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    );
}
