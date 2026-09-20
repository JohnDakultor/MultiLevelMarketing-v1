namespace modular_mlm.Application.Inventory.Commands.ReleaseOrderInventory;

public sealed record ReleaseOrderInventoryCommand(Guid OrganizationId, Guid OrderId, string Reason)
    : IRequest<int>;
