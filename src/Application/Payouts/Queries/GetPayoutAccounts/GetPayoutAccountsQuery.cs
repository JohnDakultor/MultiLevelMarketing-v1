using modular_mlm.Application.Payouts.Queries.GetPayoutAccounts.Models;

namespace modular_mlm.Application.Payouts.Queries.GetPayoutAccounts;

public sealed record GetPayoutAccountsQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<IReadOnlyList<PayoutAccountDto>>,
        IAgentScopedRequest;
