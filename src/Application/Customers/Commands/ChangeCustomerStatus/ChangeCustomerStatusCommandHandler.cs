using modular_mlm.Application.Common.Auditing;
using modular_mlm.Domain.Identity;

namespace modular_mlm.Application.Customers.Commands.ChangeCustomerStatus;

public sealed class ChangeCustomerStatusCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<ChangeCustomerStatusCommand>
{
    public async Task Handle(
        ChangeCustomerStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        var customer = await db.CustomerProfiles.SingleOrDefaultAsync(
            candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.Id == request.CustomerId,
            cancellationToken
        );
        if (customer is null)
            throw new KeyNotFoundException("Customer was not found.");

        var before = AuditJson.Serialize(new { customer.Status });
        switch (request.Status)
        {
            case CustomerStatus.Active:
                customer.Reactivate();
                break;
            case CustomerStatus.Suspended:
                customer.Suspend();
                break;
            case CustomerStatus.Disabled:
                customer.Disable();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Status));
        }

        var audit = AuditCoverageMap.CustomerStatusChanged;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            customer.Id,
            before,
            AuditJson.Serialize(new { customer.Status }),
            request.Reason.Trim()
        );
        await db.SaveChangesAsync(cancellationToken);
    }
}
