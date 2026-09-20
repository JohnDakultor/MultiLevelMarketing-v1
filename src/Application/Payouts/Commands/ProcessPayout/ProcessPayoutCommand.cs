namespace modular_mlm.Application.Payouts.Commands.ProcessPayout;

public sealed record ProcessPayoutCommand(Guid OrganizationId, Guid PayoutRequestId)
    : IRequest<string>,
        IOrganizationAdminRequest;
