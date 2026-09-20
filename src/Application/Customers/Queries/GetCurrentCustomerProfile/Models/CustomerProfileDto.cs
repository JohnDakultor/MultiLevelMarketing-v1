namespace modular_mlm.Application.Customers.Queries.GetCurrentCustomerProfile.Models;

public sealed record CustomerProfileDto(
    Guid Id,
    Guid OrganizationId,
    string DisplayName,
    Guid? DefaultAddressId
);
