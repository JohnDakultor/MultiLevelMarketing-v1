namespace modular_mlm.Application.Payouts.Commands.RequestPayout;

public sealed record RequestPayoutCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid PayoutAccountId,
    decimal Amount,
    string Currency
) : IRequest<Guid>, IAgentScopedRequest;
