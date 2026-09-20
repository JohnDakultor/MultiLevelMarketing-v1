using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary.Models;

namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary;

public sealed record GetBinaryVolumeSummaryQuery(Guid OrganizationId, Guid AgentId)
    : IRequest<BinaryVolumeSummaryDto>,
        IAgentScopedRequest;
