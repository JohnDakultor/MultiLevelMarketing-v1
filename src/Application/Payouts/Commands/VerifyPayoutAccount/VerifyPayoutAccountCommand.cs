namespace modular_mlm.Application.Payouts.Commands.VerifyPayoutAccount;

public sealed record VerifyPayoutAccountCommand(Guid OrganizationId, Guid PayoutAccountId)
    : IRequest,
        IOrganizationAdminRequest;
