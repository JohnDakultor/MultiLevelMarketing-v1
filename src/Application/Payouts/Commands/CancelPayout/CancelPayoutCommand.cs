namespace modular_mlm.Application.Payouts.Commands.CancelPayout;

public sealed record CancelPayoutCommand(Guid OrganizationId, Guid AgentId, Guid PayoutRequestId)
    : IRequest,
        IAgentScopedRequest;
