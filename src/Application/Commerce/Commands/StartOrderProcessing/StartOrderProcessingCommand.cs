namespace modular_mlm.Application.Commerce.Commands.StartOrderProcessing;

public sealed record StartOrderProcessingCommand(Guid OrganizationId, Guid OrderId)
    : IRequest,
        IOrganizationAdminRequest;
