using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Organizations.Commands.RemoveOrganizationDomain;

public sealed class RemoveOrganizationDomainCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<RemoveOrganizationDomainCommand>
{
    public async Task Handle(
        RemoveOrganizationDomainCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db
            .Organizations.Include(candidate => candidate.Domains)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == request.OrganizationId,
                cancellationToken
            );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var domain = organization.Domains.SingleOrDefault(candidate =>
            candidate.Id == request.OrganizationDomainId
        );
        if (domain is null)
            throw new KeyNotFoundException("Organization domain was not found.");

        var beforeJson = AuditJson.Serialize(
            new
            {
                domain.HostName,
                domain.IsPrimary,
                domain.VerifiedAt,
                domain.CreatedAt,
            }
        );
        organization.RemoveDomain(domain.Id);
        db.OrganizationDomains.Remove(domain);

        var audit = AuditCoverageMap.OrganizationDomainRemoved;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            domain.Id,
            beforeJson,
            afterJson: null,
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
