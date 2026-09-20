namespace modular_mlm.Application.Payouts.Commands.MakeDefaultPayoutAccount;

public sealed record MakeDefaultPayoutAccountCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid PayoutAccountId
) : IRequest, IAgentScopedRequest;
