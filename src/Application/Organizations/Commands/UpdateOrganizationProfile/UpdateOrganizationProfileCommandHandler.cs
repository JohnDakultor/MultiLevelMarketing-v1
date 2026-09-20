using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateOrganizationProfile;

public sealed class UpdateOrganizationProfileCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateOrganizationProfileCommand>
{
    public async Task Handle(
        UpdateOrganizationProfileCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization);
        organization.UpdateProfile(
            request.Name,
            request.CurrencyCode,
            request.TimeZone,
            request.Locale
        );

        var audit = AuditCoverageMap.OrganizationProfileUpdated;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(Organization organization) =>
        AuditJson.Serialize(
            new
            {
                organization.Name,
                organization.Slug,
                organization.CurrencyCode,
                organization.TimeZone,
                organization.Locale,
                organization.Status,
            }
        );
}
