using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;

namespace modular_mlm.Application.Organizations.Commands.VerifyOrganizationDomain;

public sealed class VerifyOrganizationDomainCommandHandler(
    IApplicationDbContext db,
    IDnsTxtRecordResolver dns,
    IAuditWriter auditWriter,
    TimeProvider timeProvider
) : IRequestHandler<VerifyOrganizationDomainCommand>
{
    public async Task Handle(
        VerifyOrganizationDomainCommand request,
        CancellationToken cancellationToken
    )
    {
        var domain = await db.OrganizationDomains.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.OrganizationDomainId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (domain is null)
            throw new KeyNotFoundException("Organization domain was not found.");
        if (domain.IsVerified)
            return;

        if (
            !await dns.ContainsAsync(
                domain.VerificationRecordName,
                domain.VerificationToken,
                cancellationToken
            )
        )
            throw new ConflictException(
                $"DNS TXT record '{domain.VerificationRecordName}' does not contain the expected verification value."
            );

        var beforeJson = AuditJson.Serialize(
            new { domain.HostName, domain.IsPrimary, domain.VerifiedAt }
        );
        domain.Verify(timeProvider.GetUtcNow());
        auditWriter.Write(
            request.OrganizationId,
            AuditCoverageMap.OrganizationDomainVerified.Action,
            AuditCoverageMap.OrganizationDomainVerified.EntityType,
            domain.Id,
            beforeJson,
            AuditJson.Serialize(new { domain.HostName, domain.IsPrimary, domain.VerifiedAt }),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
