using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class WebhookFailureRecorder(ApplicationDbContext db, TimeProvider clock)
    : IWebhookFailureRecorder
{
    public async Task RecordAsync(
        string provider,
        string providerEventId,
        string eventType,
        string providerResourceId,
        string resourceKind,
        string payloadHash,
        string error,
        CancellationToken cancellationToken
    )
    {
        var existing = await db.WebhookProcessingAttempts.SingleOrDefaultAsync(
            attempt => attempt.Provider == provider && attempt.ProviderEventId == providerEventId,
            cancellationToken
        );
        if (existing is not null)
        {
            existing.MarkFailed(error, clock.GetUtcNow(), 8);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Guid? organizationId = resourceKind switch
        {
            "Payment" => await db
                .Payments.Where(payment => payment.ProviderPaymentId == providerResourceId)
                .Select(payment => (Guid?)payment.OrganizationId)
                .SingleOrDefaultAsync(cancellationToken),
            "Payout" => await db
                .PayoutRequests.Where(payout => payout.ProviderTransferId == providerResourceId)
                .Select(payout => (Guid?)payout.OrganizationId)
                .SingleOrDefaultAsync(cancellationToken),
            _ => null,
        };
        var attempt = WebhookProcessingAttempt.Create(
            organizationId,
            provider,
            providerEventId,
            eventType,
            providerResourceId,
            resourceKind,
            payloadHash,
            clock.GetUtcNow()
        );
        attempt.MarkFailed(error, clock.GetUtcNow(), 8);
        db.WebhookProcessingAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
    }
}
