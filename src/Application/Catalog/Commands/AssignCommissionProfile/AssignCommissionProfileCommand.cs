namespace modular_mlm.Application.Catalog.Commands.AssignCommissionProfile;

public sealed record AssignCommissionProfileCommand(
    Guid OrganizationId,
    Guid ProductId,
    Guid? CommissionProfileId
) : IRequest, IOrganizationAdminRequest;
