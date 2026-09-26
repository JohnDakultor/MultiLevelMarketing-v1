using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetPublicOrganizationConfig.Models;
using modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost.Models;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Queries.ResolveOrganizationByHost;

public sealed class ResolveOrganizationByHostQueryHandler(IApplicationDbContext db)
    : IRequestHandler<ResolveOrganizationByHostQuery, ResolvedOrganizationDto?>
{
    public async Task<ResolvedOrganizationDto?> Handle(
        ResolveOrganizationByHostQuery request,
        CancellationToken cancellationToken
    )
    {
        var hostName = OrganizationDomain.NormalizeHostName(request.HostName);
        var resolved = await (
            from domain in db.OrganizationDomains.AsNoTracking()
            join organization in db.Organizations.AsNoTracking()
                on domain.OrganizationId equals organization.Id
            where
                domain.HostName == hostName
                && domain.VerifiedAt != null
                && organization.Status == OrganizationStatus.Active
                && organization.PublishedBranding != null
            select new ResolvedOrganizationDto(
                organization.Id,
                organization.Slug,
                domain.HostName,
                new PublicOrganizationConfigDto(
                    organization.Id,
                    organization.Name,
                    organization.Slug,
                    organization.CurrencyCode,
                    organization.Locale,
                    organization.PublishedBranding!.StoreTitle,
                    organization.PublishedBranding.PrimaryColor,
                    organization.PublishedBranding.SecondaryColor,
                    organization.PublishedBranding.AccentColor,
                    organization.PublishedBranding.LogoUrl,
                    organization.Features.AgentProgramEnabled,
                    organization.Features.BinaryNetworkEnabled,
                    organization.Features.WalletEnabled,
                    organization.Features.PayoutEnabled
                )
            )
        ).SingleOrDefaultAsync(cancellationToken);

        if (resolved is not null || string.IsNullOrWhiteSpace(request.FallbackSlug))
            return resolved;

        var fallbackSlug = request.FallbackSlug.Trim().ToLowerInvariant();
        return await db
            .Organizations.AsNoTracking()
            .Where(organization =>
                organization.Slug == fallbackSlug
                && organization.Status == OrganizationStatus.Active
                && organization.PublishedBranding != null
            )
            .Select(organization => new ResolvedOrganizationDto(
                organization.Id,
                organization.Slug,
                hostName,
                new PublicOrganizationConfigDto(
                    organization.Id,
                    organization.Name,
                    organization.Slug,
                    organization.CurrencyCode,
                    organization.Locale,
                    organization.PublishedBranding!.StoreTitle,
                    organization.PublishedBranding.PrimaryColor,
                    organization.PublishedBranding.SecondaryColor,
                    organization.PublishedBranding.AccentColor,
                    organization.PublishedBranding.LogoUrl,
                    organization.Features.AgentProgramEnabled,
                    organization.Features.BinaryNetworkEnabled,
                    organization.Features.WalletEnabled,
                    organization.Features.PayoutEnabled
                )
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
