using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface INotificationDeliveryOutbox
{
    Task StageAsync(
        NotificationDeliveryRequest request,
        DateTimeOffset notBefore,
        CancellationToken cancellationToken
    );

    Task ActivateAsync(
        Guid notificationId,
        NotificationChannel channel,
        DateTimeOffset readyAt,
        CancellationToken cancellationToken
    );
}
