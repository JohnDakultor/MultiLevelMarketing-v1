using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Organizations.Commands.PublishBranding;

public sealed class PublishBrandingCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<PublishBrandingCommand>
{
    public async Task Handle(PublishBrandingCommand request, CancellationToken cancellationToken)
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var previousRevision = organization.PublishedBrandingRevision;
        if (!organization.PublishBranding(timeProvider.GetUtcNow()))
            return;

        var audit = AuditCoverageMap.BrandingPublished;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            organization.Id,
            AuditJson.Serialize(new { PublishedBrandingRevision = previousRevision }),
            AuditJson.Serialize(
                new { organization.PublishedBrandingRevision, organization.BrandingPublishedAt }
            ),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
