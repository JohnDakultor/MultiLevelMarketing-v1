using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Catalog.Commands.UpdateProductVariant;

[Authorize(Roles = Roles.Administrator)]
public sealed record UpdateProductVariantCommand(
    Guid OrganizationId,
    Guid ProductId,
    Guid ProductVariantId,
    decimal Price,
    decimal BusinessVolume,
    decimal? Weight,
    string AttributesJson,
    bool StockKeepingEnabled,
    long ExpectedVersion
) : IRequest<Guid>, IOrganizationAdminRequest;
