using modular_mlm.Application.Network.Queries.GetAdminAgentDetails.Models;

namespace modular_mlm.Application.Network.Queries.GetAdminAgentDetails;

public sealed record GetAdminAgentDetailsQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<AdminAgentDetailsDto?>,
        IOrganizationAdminRequest;
