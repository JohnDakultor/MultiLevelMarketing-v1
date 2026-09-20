using System.Net.Mail;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class EmailNotificationSender(
    IEmailSender emailSender,
    NotificationTemplateRenderer renderer
) : INotificationSender
{
    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken
    )
    {
        RenderedNotification rendered;
        try
        {
            rendered = renderer.Render(request.TemplateKey, request.Culture, request.Variables);
        }
        catch (Exception exception)
            when (exception is ArgumentException or FormatException or KeyNotFoundException)
        {
            return NotificationDeliveryResult.PermanentFailure("INVALID_TEMPLATE");
        }

        try
        {
            await emailSender.SendAsync(
                request.RecipientAddress,
                rendered.Subject,
                rendered.PlainTextBody,
                cancellationToken
            );
            return NotificationDeliveryResult.Success();
        }
        catch (SmtpFailedRecipientException)
        {
            return NotificationDeliveryResult.PermanentFailure("INVALID_RECIPIENT");
        }
        catch (FormatException)
        {
            return NotificationDeliveryResult.PermanentFailure("INVALID_RECIPIENT");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NotificationDeliveryResult.TransientFailure("DELIVERY_TIMEOUT");
        }
        catch (SmtpException)
        {
            return NotificationDeliveryResult.TransientFailure("SMTP_UNAVAILABLE");
        }
        catch (IOException)
        {
            return NotificationDeliveryResult.TransientFailure("TRANSPORT_UNAVAILABLE");
        }
    }
}
