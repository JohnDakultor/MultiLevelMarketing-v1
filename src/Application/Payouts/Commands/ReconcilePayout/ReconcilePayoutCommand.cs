namespace modular_mlm.Application.Payouts.Commands.ReconcilePayout;

public sealed record ReconcilePayoutCommand(Guid PayoutRequestId, Guid? OrganizationId = null)
    : IRequest<bool>;
