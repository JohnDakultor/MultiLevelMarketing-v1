using modular_mlm.Application.Wallets.Queries.GetWalletSummary.Models;

namespace modular_mlm.Application.Wallets.Queries.GetWalletSummary;

public sealed record GetWalletSummaryQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<WalletSummaryDto?>,
        IAgentScopedRequest;
