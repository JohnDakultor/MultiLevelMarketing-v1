namespace modular_mlm.Application.Network.Commands.ReactivateAgent;

public sealed record ReactivateAgentCommand(Guid OrganizationId, Guid AgentId)
    : IRequest,
        IOrganizationAdminRequest;
