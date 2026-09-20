namespace modular_mlm.Application.Payouts.Commands.ReconcilePayout;

public sealed class ReconcilePayoutForOrganizationCommandHandler(ISender sender)
    : IRequestHandler<ReconcilePayoutForOrganizationCommand, bool>
{
    public Task<bool> Handle(
        ReconcilePayoutForOrganizationCommand request,
        CancellationToken cancellationToken
    ) =>
        sender.Send(
            new ReconcilePayoutCommand(request.PayoutRequestId, request.OrganizationId),
            cancellationToken
        );
}
