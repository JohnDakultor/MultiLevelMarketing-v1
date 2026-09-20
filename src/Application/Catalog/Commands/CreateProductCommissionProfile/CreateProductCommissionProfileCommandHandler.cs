using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Catalog;

namespace modular_mlm.Application.Catalog.Commands.CreateProductCommissionProfile;

public sealed class CreateProductCommissionProfileCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<CreateProductCommissionProfileCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateProductCommissionProfileCommand request,
        CancellationToken cancellationToken
    )
    {
        if (
            !await db
                .Organizations.AsNoTracking()
                .AnyAsync(
                    organization => organization.Id == request.OrganizationId,
                    cancellationToken
                )
        )
            throw new KeyNotFoundException("Organization was not found.");

        var profile = ProductCommissionProfile.Create(
            request.OrganizationId,
            request.Name,
            request.EffectiveFrom
        );
        profile.ConfigureDirectSales(request.DirectSalesEligible, request.DirectSalesRateOverride);
        profile.ConfigureBinaryVolume(request.BinaryVolumeEligible, request.BinaryVolumeOverride);
        db.ProductCommissionProfiles.Add(profile);
        var audit = AuditCoverageMap.CommissionProfileCreated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            profile.Id,
            beforeJson: null,
            AuditJson.Serialize(
                new
                {
                    profile.Name,
                    profile.DirectSalesEligible,
                    profile.DirectSalesRateOverride,
                    profile.BinaryVolumeEligible,
                    profile.BinaryVolumeOverride,
                    profile.EffectiveFrom,
                    profile.EffectiveTo,
                }
            ),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }
}
