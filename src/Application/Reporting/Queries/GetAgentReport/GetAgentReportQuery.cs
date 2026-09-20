using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;

namespace modular_mlm.Application.Reporting.Queries.GetAgentReport;

public sealed record GetAgentReportQuery(
    Guid OrganizationId,
    DateTimeOffset From,
    DateTimeOffset To
) : IRequest<AgentReportDto>, ICurrentAgentRequest;
