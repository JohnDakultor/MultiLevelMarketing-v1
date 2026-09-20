namespace modular_mlm.Application.Network.Commands.RejectAgent;

public sealed record RejectAgentCommand(Guid OrganizationId, Guid AgentId)
    : IRequest,
        IOrganizationAdminRequest;
