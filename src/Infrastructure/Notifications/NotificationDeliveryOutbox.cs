using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class NotificationDeliveryOutbox(
    ApplicationDbContext db,
    IDataProtectionProvider dataProtectionProvider
) : INotificationDeliveryOutbox
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        NotificationDeliveryEnvelope.ProtectionPurpose
    );

    public async Task StageAsync(
        NotificationDeliveryRequest request,
        DateTimeOffset notBefore,
        CancellationToken cancellationToken
    )
    {
        var payload = new NotificationDeliveryPayload(
            request.RecipientUserId,
            request.RecipientAddress,
            request.Culture,
            request.Variables
        );
        var json = JsonSerializer.Serialize(payload);
        var hash = Hash(json);
        var existing = await db.NotificationDeliveryEnvelopes.SingleOrDefaultAsync(
            envelope =>
                envelope.NotificationId == request.NotificationId
                && envelope.Channel == request.Channel,
            cancellationToken
        );
        if (existing is not null)
        {
            if (
                existing.OrganizationId != request.OrganizationId
                || existing.TemplateKey != request.TemplateKey
                || existing.PayloadHash != hash
            )
                throw new IdempotencyConflictException(
                    "The notification delivery already has different semantics."
                );
            return;
        }

        db.NotificationDeliveryEnvelopes.Add(
            NotificationDeliveryEnvelope.Stage(
                request.OrganizationId,
                request.NotificationId,
                null,
                request.Channel,
                request.TemplateKey,
                _protector.Protect(json),
                hash,
                $"notification:{request.NotificationId:N}:{request.Channel}",
                notBefore
            )
        );
    }

    public async Task ActivateAsync(
        Guid notificationId,
        NotificationChannel channel,
        DateTimeOffset readyAt,
        CancellationToken cancellationToken
    )
    {
        await db
            .NotificationDeliveryEnvelopes.Where(envelope =>
                envelope.NotificationId == notificationId
                && envelope.Channel == channel
                && envelope.ReadyAt == null
                && envelope.DeliveredAt == null
                && envelope.DeadLetteredAt == null
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(envelope => envelope.ReadyAt, readyAt)
                        .SetProperty(envelope => envelope.NextAttemptAt, readyAt),
                cancellationToken
            );
    }

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
