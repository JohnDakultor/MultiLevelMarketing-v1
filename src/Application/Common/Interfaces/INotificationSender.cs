using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface INotificationSender
{
    Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken
    );
}
