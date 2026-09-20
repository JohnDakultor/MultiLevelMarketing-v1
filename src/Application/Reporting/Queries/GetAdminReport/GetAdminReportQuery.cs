using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;

namespace modular_mlm.Application.Reporting.Queries.GetAdminReport;

public sealed record GetAdminReportQuery(
    Guid OrganizationId,
    DateTimeOffset From,
    DateTimeOffset To
) : IRequest<AdminReportDto>, IOrganizationAdminRequest;
