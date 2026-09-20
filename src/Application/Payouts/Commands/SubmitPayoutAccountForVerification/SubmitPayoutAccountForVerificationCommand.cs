namespace modular_mlm.Application.Payouts.Commands.SubmitPayoutAccountForVerification;

public sealed record SubmitPayoutAccountForVerificationCommand(
    Guid OrganizationId,
    Guid AgentId,
    Guid PayoutAccountId
) : IRequest, IAgentScopedRequest;
