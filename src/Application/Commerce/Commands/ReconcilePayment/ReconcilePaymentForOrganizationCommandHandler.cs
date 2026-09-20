namespace modular_mlm.Application.Commerce.Commands.ReconcilePayment;

public sealed class ReconcilePaymentForOrganizationCommandHandler(ISender sender)
    : IRequestHandler<ReconcilePaymentForOrganizationCommand, bool>
{
    public Task<bool> Handle(
        ReconcilePaymentForOrganizationCommand request,
        CancellationToken cancellationToken
    ) =>
        sender.Send(
            new ReconcilePaymentCommand(request.OrganizationId, request.PaymentId),
            cancellationToken
        );
}
