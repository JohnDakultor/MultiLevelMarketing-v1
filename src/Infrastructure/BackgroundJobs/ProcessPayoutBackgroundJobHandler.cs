using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Application.Payouts.Commands.ProcessPayout;
using modular_mlm.Application.Payouts.Models;
using modular_mlm.Domain.Payouts;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class ProcessPayoutBackgroundJobHandler(
    ApplicationDbContext db,
    ProcessPayoutCommandHandler handler
) : IBackgroundJobHandler
{
    public string JobName => DurableJobRegistry.ProcessCommissionPayout;

    public async Task HandleAsync(DurableJobPayload payload, CancellationToken cancellationToken)
    {
        var request =
            JsonSerializer.Deserialize<ProcessPayoutJobPayload>(payload.PayloadJson)
            ?? throw new InvalidOperationException("Payout job payload is invalid.");
        if (
            payload.OrganizationId != request.OrganizationId
            || payload.IdempotencyKey != request.IdempotencyKey
        )
            throw new InvalidOperationException("Payout job identity does not match its envelope.");

        var state = await db
            .PayoutRequests.AsNoTracking()
            .Where(payout =>
                payout.Id == request.PayoutRequestId
                && payout.OrganizationId == request.OrganizationId
            )
            .Select(payout => (PayoutStatus?)payout.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (state is null)
            throw new KeyNotFoundException("Payout request was not found.");
        if (state is PayoutStatus.Paid or PayoutStatus.Failed)
            return;
        if (state is not (PayoutStatus.Approved or PayoutStatus.Processing))
            throw new InvalidOperationException(
                "Payout is not eligible for background processing."
            );

        await handler.HandleTrustedAsync(
            new ProcessPayoutCommand(request.OrganizationId, request.PayoutRequestId),
            cancellationToken
        );
    }
}
