using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings.Models;

namespace modular_mlm.Application.Organizations.Queries.GetAdminOrganizationSettings;

public sealed class GetAdminOrganizationSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAdminOrganizationSettingsQuery, AdminOrganizationSettingsDto?>
{
    public async Task<AdminOrganizationSettingsDto?> Handle(
        GetAdminOrganizationSettingsQuery request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db
            .Organizations.AsNoTracking()
            .Include(candidate => candidate.Domains)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );
        if (organization is null)
            return null;

        return new AdminOrganizationSettingsDto(
            new OrganizationProfileDto(
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.Status,
                organization.CurrencyCode,
                organization.TimeZone,
                organization.Locale
            ),
            new BrandingSettingsDto(
                organization.Branding.LogoUrl,
                organization.Branding.FaviconUrl,
                organization.Branding.PrimaryColor,
                organization.Branding.SecondaryColor,
                organization.Branding.AccentColor,
                organization.Branding.StoreTitle,
                organization.Branding.SupportEmail,
                organization.Branding.SupportPhone,
                organization.Branding.FooterText,
                organization.BrandingRevision,
                organization.PublishedBrandingRevision,
                organization.BrandingPublishedAt
            ),
            new FeatureSettingsDto(
                organization.Features.CommerceEnabled,
                organization.Features.AgentProgramEnabled,
                organization.Features.BinaryNetworkEnabled,
                organization.Features.BinaryPairingEnabled,
                organization.Features.WalletEnabled,
                organization.Features.PayoutEnabled,
                organization.Features.ReviewsEnabled,
                organization.Features.CouponsEnabled
            ),
            new CommerceSettingsDto(
                organization.Commerce.AllowGuestCheckout,
                organization.Commerce.RequireShippingAddress,
                organization.Commerce.RequireBillingAddress,
                organization.Commerce.InventoryReservationMinutes
            ),
            new NetworkSettingsDto(
                organization.Network.DefaultPlacementStrategy,
                organization.Network.AllowAgentPreferredLeg,
                organization.Network.MaxQueryDepth,
                organization.Network.AutoPlacementEnabled,
                organization.Network.RestrictPlacementChangesAfterActivation
            ),
            organization
                .Domains.OrderByDescending(domain => domain.IsPrimary)
                .ThenBy(domain => domain.HostName)
                .Select(domain => new OrganizationDomainDto(
                    domain.Id,
                    domain.HostName,
                    domain.IsPrimary,
                    domain.IsVerified,
                    domain.CreatedAt,
                    domain.VerifiedAt
                ))
                .ToArray()
        );
    }
}
