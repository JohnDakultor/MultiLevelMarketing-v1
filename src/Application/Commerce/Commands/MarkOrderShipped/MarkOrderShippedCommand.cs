namespace modular_mlm.Application.Commerce.Commands.MarkOrderShipped;

public sealed record MarkOrderShippedCommand(
    Guid OrganizationId,
    Guid OrderId,
    string? Carrier,
    string? TrackingNumber
) : IRequest, IOrganizationAdminRequest;
