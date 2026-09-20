// PHASE 6 PSEUDOCODE ONLY
// Namespace: modular_mlm.Application.Common.Exceptions
// DEFINE a stable Application exception for optimistic inventory conflicts.
// Include ProductVariantId and a safe message instructing the caller to reload and retry.
// Never expose provider, SQL, xmin, or stack-trace details.
// Map this exception to HTTP 409 in ProblemDetailsExceptionHandler.

namespace modular_mlm.Application.Common.Exceptions;

public sealed class InventoryConcurrencyConflictException(Guid productVariantId)
    : Exception(
        "The inventory for this product variant was updated by another process. Please reload the current stock allocation and try again."
    )
{
    public Guid ProductVariantId { get; } = productVariantId;
}
