using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.ArchiveProductVariant;

public sealed class ArchiveProductVariantCommandHandler(
    IApplicationDbContext db,
    IUser user,
    IAuditWriter writer
) : IRequestHandler<ArchiveProductVariantCommand, Guid>
{
    public async Task<Guid> Handle(
        ArchiveProductVariantCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = user.UserId;
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var product = await db
            .Products.Include(candidate => candidate.Variants)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == request.ProductId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken
            );
        if (product is null)
            throw new KeyNotFoundException("Product was not found in this organization.");

        var variant = product.Variants.SingleOrDefault(candidate =>
            candidate.Id == request.ProductVariantId
        );
        if (variant is null)
            throw new KeyNotFoundException("Product variant was not found in this product.");
        if (variant.Status == ProductStatus.Archived)
            return variant.Id;
        if (variant.Version != request.ExpectedVersion)
            throw new InventoryConcurrencyConflictException(variant.Id);

        var hasActiveReservations = await db.InventoryReservations.AnyAsync(
            reservation =>
                reservation.OrganizationId == request.OrganizationId
                && reservation.ProductVariantId == variant.Id
                && reservation.Status == InventoryReservationStatus.Active,
            cancellationToken
        );
        if (hasActiveReservations)
            throw new InvalidOperationException(
                "A variant with active reservations cannot be archived."
            );

        var before = AuditJson.Serialize(new { variant.Status, variant.Version });

        variant.Archive();

        var audit = AuditCoverageMap.ProductVariantArchived;

        writer.WriteAsActor(
            request.OrganizationId,
            userId,
            audit.Action,
            audit.EntityType,
            variant.Id,
            before,
            AuditJson.Serialize(new { variant.Status, variant.Version }),
            request.Reason.Trim()
        );

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InventoryConcurrencyConflictException(variant.Id);
        }

        return variant.Id;
    }
}
