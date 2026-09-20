namespace modular_mlm.Application.Network.Commands.ApproveAgent;

public sealed record ApproveAgentCommand(Guid OrganizationId, Guid AgentId)
    : IRequest,
        IOrganizationAdminRequest;
