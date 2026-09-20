using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Catalog.Commands.AssignCommissionProfile;

public sealed class AssignCommissionProfileCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<AssignCommissionProfileCommand>
{
    public async Task Handle(
        AssignCommissionProfileCommand request,
        CancellationToken cancellationToken
    )
    {
        var product = await db.Products.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.ProductId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (product is null)
            throw new KeyNotFoundException("Product was not found.");

        if (
            request.CommissionProfileId.HasValue
            && !await db
                .ProductCommissionProfiles.AsNoTracking()
                .AnyAsync(
                    profile =>
                        profile.Id == request.CommissionProfileId
                        && profile.OrganizationId == request.OrganizationId,
                    cancellationToken
                )
        )
            throw new KeyNotFoundException("Product commission profile was not found.");

        var beforeJson = AuditJson.Serialize(new { product.CommissionProfileId });
        product.AssignCommissionProfile(request.CommissionProfileId);
        var audit = AuditCoverageMap.CommissionProfileAssigned;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            product.Id,
            beforeJson,
            AuditJson.Serialize(new { product.CommissionProfileId }),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
