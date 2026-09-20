using modular_mlm.Application.Commerce.Commands.ReconcilePayment;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Commerce.Commands.ReconcilePaymentByProvider;

public sealed class ReconcilePaymentByProviderCommandHandler(
    IApplicationDbContext db,
    ISender sender
) : IRequestHandler<ReconcilePaymentByProviderCommand, bool>
{
    public async Task<bool> Handle(
        ReconcilePaymentByProviderCommand request,
        CancellationToken cancellationToken
    )
    {
        var payment = await db
            .Payments.AsNoTracking()
            .Where(candidate => candidate.ProviderPaymentId == request.ProviderPaymentId)
            .Select(candidate => new { candidate.Id, candidate.OrganizationId })
            .SingleOrDefaultAsync(cancellationToken);
        if (payment is null)
            throw new KeyNotFoundException("PayMongo payment is unknown.");
        return await sender.Send(
            new ReconcilePaymentCommand(payment.OrganizationId, payment.Id),
            cancellationToken
        );
    }
}
