namespace modular_mlm.Application.Network.Commands.SuspendAgent;

public sealed record SuspendAgentCommand(Guid OrganizationId, Guid AgentId)
    : IRequest,
        IOrganizationAdminRequest;
