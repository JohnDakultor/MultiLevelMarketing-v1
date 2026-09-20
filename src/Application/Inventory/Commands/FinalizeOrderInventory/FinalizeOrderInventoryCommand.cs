namespace modular_mlm.Application.Inventory.Commands.FinalizeOrderInventory;

public sealed record FinalizeOrderInventoryCommand(Guid OrganizationId, Guid OrderId)
    : IRequest<int>;
