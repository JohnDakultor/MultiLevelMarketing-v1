namespace modular_mlm.Application.Inventory.Commands.ReleaseExpiredInventory;

public sealed record ReleaseExpiredInventoryCommand(
    Guid OrganizationId,
    int BatchSize,
    DateTimeOffset Now
) : IRequest<ReleaseExpiredInventoryResult>;

public sealed record ReleaseExpiredInventoryResult(
    int ProcessedCount,
    int ReleasedCount,
    int FailureCount
);
