namespace modular_mlm.Application.Commerce.Commands.AdminCancelOrder;

public sealed record AdminCancelOrderCommand(Guid OrganizationId, Guid OrderId, string Reason)
    : IRequest,
        IOrganizationAdminRequest;
