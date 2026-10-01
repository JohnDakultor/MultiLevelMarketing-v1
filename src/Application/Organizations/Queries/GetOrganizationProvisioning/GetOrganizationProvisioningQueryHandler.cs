using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning.Models;

namespace modular_mlm.Application.Organizations.Queries.GetOrganizationProvisioning;

public sealed class GetOrganizationProvisioningQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrganizationProvisioningQuery, OrganizationProvisioningDto?>
{
    public Task<OrganizationProvisioningDto?> Handle(
        GetOrganizationProvisioningQuery request,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        return db
            .Organizations.AsNoTracking()
            .Where(organization => organization.Slug == normalizedSlug)
            .Select(organization => new OrganizationProvisioningDto(
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.PublishedBrandingRevision > 0
            ))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
