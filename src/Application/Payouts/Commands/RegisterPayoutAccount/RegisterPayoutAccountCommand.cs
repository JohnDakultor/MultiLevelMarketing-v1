namespace modular_mlm.Application.Payouts.Commands.RegisterPayoutAccount;

public sealed record RegisterPayoutAccountCommand(
    Guid OrganizationId,
    Guid AgentId,
    string Method,
    string AccountName,
    string AccountNumber,
    string BankCode,
    string Rail
) : IRequest<Guid>, IAgentScopedRequest;
