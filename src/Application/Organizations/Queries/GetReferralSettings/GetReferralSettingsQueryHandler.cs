using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetReferralSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetReferralSettings;

public sealed class GetReferralSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReferralSettingsQuery, ReferralSettingsDto>
{
    public async Task<ReferralSettingsDto> Handle(
        GetReferralSettingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var settings = await db
            .Organizations.AsNoTracking()
            .Where(organization => organization.Id == request.OrganizationId)
            .Select(organization => new ReferralSettingsDto(
                organization.Referrals.AttributionWindowDays,
                organization.Referrals.AllowReferralOverride,
                organization.Referrals.ReferralLockAfterFirstPurchase
            ))
            .SingleOrDefaultAsync(cancellationToken);

        return settings ?? throw new KeyNotFoundException("Organization was not found.");
    }
}
