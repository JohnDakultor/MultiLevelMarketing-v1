using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateBranding;

public sealed class UpdateBrandingCommandHandler(IApplicationDbContext db, IAuditWriter auditWriter)
    : IRequestHandler<UpdateBrandingCommand>
{
    public async Task Handle(UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            x => x.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Branding);
        organization.UpdateBranding(
            BrandingSettings.Create(
                request.StoreTitle,
                request.SupportEmail,
                request.PrimaryColor,
                request.SecondaryColor,
                request.AccentColor,
                request.LogoUrl,
                request.FaviconUrl,
                request.SupportPhone,
                request.FooterText
            )
        );
        var audit = AuditCoverageMap.BrandingUpdated;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Branding),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(BrandingSettings value) =>
        AuditJson.Serialize(
            new
            {
                value.LogoUrl,
                value.FaviconUrl,
                value.PrimaryColor,
                value.SecondaryColor,
                value.AccentColor,
                value.StoreTitle,
                value.SupportEmail,
                value.SupportPhone,
                value.FooterText,
            }
        );
}
