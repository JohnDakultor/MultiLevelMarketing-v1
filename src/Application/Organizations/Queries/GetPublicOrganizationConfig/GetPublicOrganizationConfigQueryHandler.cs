using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig;

public sealed class GetPublicOrganizationConfigQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPublicOrganizationConfigQuery, PublicOrganizationConfigDto?>
{
    public Task<PublicOrganizationConfigDto?> Handle(
        GetPublicOrganizationConfigQuery request,
        CancellationToken cancellationToken
    ) =>
        db
            .Organizations.AsNoTracking()
            .Where(x =>
                x.Slug == request.Slug
                && x.Status == OrganizationStatus.Active
                && x.PublishedBranding != null
            )
            .Select(x => new PublicOrganizationConfigDto(
                x.Id,
                x.Name,
                x.Slug,
                x.CurrencyCode,
                x.Locale,
                x.PublishedBranding!.StoreTitle,
                x.PublishedBranding.PrimaryColor,
                x.PublishedBranding.SecondaryColor,
                x.PublishedBranding.AccentColor,
                x.PublishedBranding.LogoUrl,
                x.Features.AgentProgramEnabled,
                x.Features.BinaryNetworkEnabled,
                x.Features.WalletEnabled,
                x.Features.PayoutEnabled
            ))
            .SingleOrDefaultAsync(cancellationToken);
}
