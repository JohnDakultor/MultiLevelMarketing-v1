using modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles.Models;

namespace modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles;

public sealed class GetProductCommissionProfilesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductCommissionProfilesQuery, IReadOnlyList<ProductCommissionProfileDto>>
{
    public async Task<IReadOnlyList<ProductCommissionProfileDto>> Handle(
        GetProductCommissionProfilesQuery request,
        CancellationToken cancellationToken
    ) =>
        await db
            .ProductCommissionProfiles.AsNoTracking()
            .Where(profile => profile.OrganizationId == request.OrganizationId)
            .OrderBy(profile => profile.Name)
            .ThenBy(profile => profile.Id)
            .Select(profile => new ProductCommissionProfileDto(
                profile.Id,
                profile.Name,
                profile.DirectSalesEligible,
                profile.DirectSalesRateOverride,
                profile.BinaryVolumeEligible,
                profile.BinaryVolumeOverride,
                profile.EffectiveFrom,
                profile.EffectiveTo
            ))
            .ToListAsync(cancellationToken);
}
