using modular_mlm.Application.Inventory.Queries.GetInventoryHistory.Models;

namespace modular_mlm.Application.Inventory.Queries.GetInventoryHistory;

public sealed record GetInventoryHistoryQuery(
    Guid OrganizationId,
    Guid ProductVariantId,
    int Page = 1,
    int PageSize = 20
) : IRequest<InventoryHistoryPageDto>, IOrganizationAdminRequest;
