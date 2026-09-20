using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Reporting.Models;

namespace modular_mlm.Application.Reporting.Queries.GetAdminDashboard;

public sealed record GetAdminDashboardQuery(Guid OrganizationId)
    : IRequest<AdminDashboardDto>,
        IOrganizationAdminRequest;
