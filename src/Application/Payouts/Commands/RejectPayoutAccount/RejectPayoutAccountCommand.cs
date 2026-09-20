namespace modular_mlm.Application.Payouts.Commands.RejectPayoutAccount;

public sealed record RejectPayoutAccountCommand(Guid OrganizationId, Guid PayoutAccountId)
    : IRequest,
        IOrganizationAdminRequest;
