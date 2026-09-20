using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;
using modular_mlm.Domain.Commerce;

namespace modular_mlm.Application.Compensation.Commands.ProcessOutstandingPaidOrderCompensation;

public sealed class ProcessOutstandingPaidOrderCompensationCommandHandler(
    IApplicationDbContext db,
    ISender sender,
    TimeProvider clock,
    ILogger<ProcessOutstandingPaidOrderCompensationCommandHandler> logger
) : IRequestHandler<ProcessOutstandingPaidOrderCompensationCommand, int>
{
    public async Task<int> Handle(
        ProcessOutstandingPaidOrderCompensationCommand request,
        CancellationToken cancellationToken
    )
    {
        var paymentIds = await db
            .Payments.AsNoTracking()
            .Where(payment =>
                payment.Status == PaymentStatus.Paid && payment.CompensationProcessedAt == null
            )
            .OrderBy(payment => payment.PaidAt)
            .Select(payment => payment.Id)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);
        var processed = 0;

        foreach (var paymentId in paymentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payment = await db.Payments.SingleAsync(
                candidate => candidate.Id == paymentId,
                cancellationToken
            );
            try
            {
                await sender.Send(
                    new ProcessPaidOrderCommissionsCommand(payment.OrganizationId, payment.OrderId),
                    cancellationToken
                );
                payment.MarkCompensationProcessed(clock.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Unable to process compensation for paid payment {PaymentId}",
                    paymentId
                );
            }
        }

        return processed;
    }
}
