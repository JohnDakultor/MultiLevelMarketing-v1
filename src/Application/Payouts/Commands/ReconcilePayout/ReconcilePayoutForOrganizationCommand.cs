namespace modular_mlm.Application.Payouts.Commands.ReconcilePayout;

public sealed record ReconcilePayoutForOrganizationCommand(
    Guid OrganizationId,
    Guid PayoutRequestId
) : IRequest<bool>, IOrganizationAdminRequest;
