using modular_mlm.Application.Monitoring.Queries.GetOperationalHealth.Models;

namespace modular_mlm.Application.Monitoring.Queries.GetOperationalHealth;

public sealed record GetOperationalHealthQuery(Guid OrganizationId)
    : IRequest<OperationalHealthDto>,
        IOrganizationAdminRequest;
