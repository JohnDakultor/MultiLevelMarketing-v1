using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Application.Payouts.Commands.ApprovePayout;

public sealed class ApprovePayoutCommandHandler(
    IUser currentUser,
    IAdministratorAccountService administrators,
    IApplicationDbContext db,
    IAuditWriter auditWriter,
    TimeProvider clock
) : IRequestHandler<ApprovePayoutCommand>
{
    public async Task Handle(ApprovePayoutCommand request, CancellationToken cancellationToken)
    {
        await RequireAdministratorAsync(request.OrganizationId, cancellationToken);
        var payout = await db.PayoutRequests.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == request.PayoutRequestId
                && candidate.OrganizationId == request.OrganizationId,
            cancellationToken
        );
        if (payout is null)
            throw new KeyNotFoundException("Payout request was not found.");
        var beforeJson = AuditJson.Serialize(new { payout.Status, payout.ApprovedAt });
        payout.Approve(clock.GetUtcNow());
        var audit = AuditCoverageMap.PayoutApproved;
        auditWriter.Write(
            request.OrganizationId,
            audit.Action,
            audit.EntityType,
            payout.Id,
            beforeJson,
            AuditJson.Serialize(new { payout.Status, payout.ApprovedAt }),
            null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireAdministratorAsync(Guid organizationId, CancellationToken token)
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (!await administrators.CanManageOrganizationAsync(userId, organizationId, token))
            throw new ForbiddenAccessException();
    }
}
