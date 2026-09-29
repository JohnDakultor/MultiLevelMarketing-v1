using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Commands.ChangeCustomerStatus;

public sealed record ChangeCustomerStatusCommand(
    Guid OrganizationId,
    Guid CustomerId,
    CustomerStatus Status,
    string Reason
) : IRequest, IOrganizationAdminRequest;
