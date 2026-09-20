using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.ConfigureOrganizationDomain;

public sealed class ConfigureOrganizationDomainCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<ConfigureOrganizationDomainCommand, Guid>
{
    public async Task<Guid> Handle(
        ConfigureOrganizationDomainCommand request,
        CancellationToken cancellationToken
    )
    {
        var normalizedHostName = OrganizationDomain.NormalizeHostName(request.HostName);
        var foreignOwnerExists = await db.OrganizationDomains.AnyAsync(
            domain =>
                domain.HostName == normalizedHostName
                && domain.OrganizationId != request.OrganizationId,
            cancellationToken
        );
        if (foreignOwnerExists)
            throw new ConflictException("The hostname is already assigned to an organization.");

        var organization = await db
            .Organizations.Include(candidate => candidate.Domains)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var existing = organization.Domains.SingleOrDefault(domain =>
            domain.HostName == normalizedHostName
        );
        var wasPrimary = existing?.IsPrimary;
        var domain = organization.ConfigureDomain(
            normalizedHostName,
            request.MakePrimary,
            timeProvider.GetUtcNow()
        );

        if (existing is not null && wasPrimary == domain.IsPrimary)
            return domain.Id;

        var audit = AuditCoverageMap.OrganizationDomainConfigured;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            domain.Id,
            existing is null
                ? null
                : AuditJson.Serialize(
                    new
                    {
                        domain.HostName,
                        IsPrimary = wasPrimary,
                        domain.VerifiedAt,
                    }
                ),
            Serialize(domain),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
        return domain.Id;
    }

    private static string Serialize(OrganizationDomain domain) =>
        AuditJson.Serialize(
            new
            {
                domain.HostName,
                domain.IsPrimary,
                domain.VerifiedAt,
                domain.CreatedAt,
            }
        );
}
