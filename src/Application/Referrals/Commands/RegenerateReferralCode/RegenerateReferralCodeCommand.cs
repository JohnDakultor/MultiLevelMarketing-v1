namespace modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;

public sealed record RegenerateReferralCodeCommand(Guid OrganizationId, Guid AgentId)
    : IRequest<string>,
        IAgentScopedRequest;
