using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Auditing;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Network.Commands.AdminMoveUncommittedPlacement;

public sealed class AdminMoveUncommittedPlacementCommandHandler(
    IApplicationDbContext db,
    IAgentPlacementMutationRepository placementRepository,
    PlacementMoveEligibilityPolicy eligibilityPolicy,
    IAuditWriter auditWriter,
    IUser user,
    TimeProvider timeProvider
) : IRequestHandler<AdminMoveUncommittedPlacementCommand>
{
    public async Task Handle(
        AdminMoveUncommittedPlacementCommand request,
        CancellationToken cancellationToken
    )
    {
        var actorUserId = user.Id ?? throw new UnauthorizedAccessException();
        var currentAgent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                agent =>
                    agent.OrganizationId == request.OrganizationId && agent.Id == request.AgentId,
                cancellationToken
            );
        if (currentAgent is null)
            throw new KeyNotFoundException("Agent was not found.");
        var beforeJson = AgentAuditSnapshot.Serialize(currentAgent);
        var descendants = await db.PlacementClosures.CountAsync(
            closure =>
                closure.OrganizationId == request.OrganizationId
                && closure.AncestorAgentId == request.AgentId,
            cancellationToken
        );
        var paidOrders = await db.Orders.CountAsync(
            order =>
                order.OrganizationId == request.OrganizationId
                && order.AttributedAgentId == request.AgentId
                && (
                    order.PaymentStatus == PaymentStatus.Paid
                    || order.PaymentStatus == PaymentStatus.PartiallyRefunded
                    || order.PaymentStatus == PaymentStatus.Refunded
                ),
            cancellationToken
        );
        var volumeEntries = await db.BinaryVolumeEntries.CountAsync(
            entry =>
                entry.OrganizationId == request.OrganizationId
                && entry.SourceAgentId == request.AgentId,
            cancellationToken
        );
        var commissions = await db.CommissionTransactions.CountAsync(
            entry =>
                entry.OrganizationId == request.OrganizationId
                && (
                    entry.BeneficiaryAgentId == request.AgentId
                    || entry.SourceAgentId == request.AgentId
                ),
            cancellationToken
        );
        var eligibility = eligibilityPolicy.Evaluate(
            new PlacementCommitmentSnapshot(descendants, paidOrders, volumeEntries, commissions)
        );
        if (!eligibility.IsEligible)
            throw new PlacementConflictException(eligibility.Reason);
        var moved = await placementRepository.TryMoveUncommittedAsync(
            request.OrganizationId,
            request.AgentId,
            request.NewParentAgentId,
            request.NewSide,
            request.ExpectedCurrentParentAgentId,
            request.ExpectedCurrentSide,
            actorUserId,
            request.Reason,
            timeProvider.GetUtcNow(),
            agent =>
            {
                var audit = AuditCoverageMap.AgentPlacementMoved;
                auditWriter.Write(
                    request.OrganizationId,
                    audit.Action,
                    audit.EntityType,
                    agent.Id,
                    beforeJson,
                    AgentAuditSnapshot.Serialize(agent),
                    request.Reason
                );
            },
            cancellationToken
        );
        if (!moved)
            throw new PlacementConflictException();
    }
}
