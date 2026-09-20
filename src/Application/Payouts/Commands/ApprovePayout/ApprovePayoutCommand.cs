namespace modular_mlm.Application.Payouts.Commands.ApprovePayout;

public sealed record ApprovePayoutCommand(Guid OrganizationId, Guid PayoutRequestId)
    : IRequest,
        IOrganizationAdminRequest;
