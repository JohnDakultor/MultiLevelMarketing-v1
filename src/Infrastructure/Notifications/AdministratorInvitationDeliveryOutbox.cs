using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Infrastructure.Notifications;

public sealed class AdministratorInvitationDeliveryOutbox(
    ApplicationDbContext db,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<AdministratorInvitationOptions> options,
    TimeProvider clock
) : IInvitationDeliveryOutbox
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        NotificationDeliveryEnvelope.ProtectionPurpose
    );

    public async Task StageAsync(
        Guid invitationId,
        Guid organizationId,
        string recipient,
        string rawToken,
        string templateKey,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken
    )
    {
        var baseUrl = options.Value.AcceptanceBaseUrl;
        var separator = string.IsNullOrEmpty(baseUrl.Query) ? "?" : "&";
        var url =
            $"{baseUrl}{separator}invitationId={invitationId:D}&token={Uri.EscapeDataString(rawToken)}";
        var payload = new NotificationDeliveryPayload(
            $"invitation:{invitationId:N}",
            recipient,
            "en-PH",
            new Dictionary<string, string> { ["invitationUrl"] = url }
        );
        var json = JsonSerializer.Serialize(payload);
        var hash = NotificationDeliveryOutbox.Hash(json);
        var existing = await db.NotificationDeliveryEnvelopes.SingleOrDefaultAsync(
            envelope => envelope.InvitationId == invitationId,
            cancellationToken
        );
        if (existing is not null)
        {
            if (
                existing.OrganizationId != organizationId
            )
                throw new IdempotencyConflictException(
                    "The invitation delivery already has different semantics."
                );
            if (existing.TemplateKey == templateKey && existing.PayloadHash == hash)
                return;

            existing.Restage(
                templateKey,
                _protector.Protect(json),
                hash,
                $"administrator-invitation:{invitationId:N}:{hash[..12]}",
                clock.GetUtcNow(),
                expiresAt
            );
            return;
        }

        db.NotificationDeliveryEnvelopes.Add(
            NotificationDeliveryEnvelope.Stage(
                organizationId,
                null,
                invitationId,
                NotificationChannel.Email,
                templateKey,
                _protector.Protect(json),
                hash,
                $"administrator-invitation:{invitationId:N}:{hash[..12]}",
                clock.GetUtcNow(),
                expiresAt
            )
        );
    }

    public async Task ActivateAsync(
        Guid organizationId,
        Guid invitationId,
        DateTimeOffset readyAt,
        CancellationToken cancellationToken
    )
    {
        await db
            .NotificationDeliveryEnvelopes.Where(envelope =>
                envelope.OrganizationId == organizationId
                && envelope.InvitationId == invitationId
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
}
