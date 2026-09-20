using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.CreateOrganization;

public sealed class CreateOrganizationCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<CreateOrganizationCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        var slugExists = await db.Organizations.AnyAsync(
            organization => organization.Slug == normalizedSlug,
            cancellationToken
        );
        if (slugExists)
            throw new ConflictException("The organization slug is already in use.");

        var organization = Organization.Create(
            request.Name,
            normalizedSlug,
            request.CurrencyCode,
            request.TimeZone,
            request.Locale,
            timeProvider.GetUtcNow()
        );
        db.Organizations.Add(organization);

        var audit = AuditCoverageMap.OrganizationCreated;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson: null,
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
            ),
            reason: null
        );

        await db.SaveChangesAsync(cancellationToken);
        return organization.Id;
    }
}
