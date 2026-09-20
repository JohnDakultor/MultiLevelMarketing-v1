namespace modular_mlm.Application.Network.Commands.ActivateAgent;

public sealed record ActivateAgentCommand(Guid OrganizationId, Guid AgentId)
    : IRequest,
        IOrganizationAdminRequest;
