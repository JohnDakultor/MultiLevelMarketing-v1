using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Catalog;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Catalog.Commands.AdjustInventory;

[Authorize(Roles = Roles.Administrator)]
public record AdjustInventoryCommand(
    Guid OrganizationId,
    Guid ProductVariantId,
    int QuantityDelta,
    InventoryAdjustmentType AdjustmentType,
    string Reason,
    string IdempotencyKey,
    int ExpectedVersion
) : IRequest<Guid>, IOrganizationAdminRequest;
