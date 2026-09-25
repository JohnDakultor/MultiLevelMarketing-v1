namespace modular_mlm.Application.Commerce.Commands.MarkOrderDelivered;

public sealed record MarkOrderDeliveredCommand(Guid OrganizationId, Guid OrderId)
    : IRequest,
        IOrganizationAdminRequest;
