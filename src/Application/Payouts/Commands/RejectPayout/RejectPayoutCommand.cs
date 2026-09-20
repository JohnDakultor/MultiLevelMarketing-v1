namespace modular_mlm.Application.Payouts.Commands.RejectPayout;

public sealed record RejectPayoutCommand(Guid OrganizationId, Guid PayoutRequestId)
    : IRequest,
        IOrganizationAdminRequest;
