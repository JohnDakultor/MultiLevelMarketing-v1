using modular_mlm.Application.Inventory.Queries.GetInventory.Models;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Inventory.Queries.GetInventory;

public sealed record GetInventoryQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ProductStatus? Status = null,
    bool LowStockOnly = false
) : IRequest<InventoryPageDto>, IOrganizationAdminRequest;
