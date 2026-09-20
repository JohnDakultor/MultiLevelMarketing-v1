using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Network.Queries.GetAdminAgentDetails.Models;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Services;
using modular_mlm.Domain.ValueObjects;

namespace modular_mlm.Application.Network.Queries.GetAdminAgentDetails;

public sealed class GetAdminAgentDetailsQueryHandler(
    IApplicationDbContext db,
    IAgentIdentityReader identityReader,
    PlacementMoveEligibilityPolicy movePolicy
) : IRequestHandler<GetAdminAgentDetailsQuery, AdminAgentDetailsDto?>
{
    public async Task<AdminAgentDetailsDto?> Handle(
        GetAdminAgentDetailsQuery request,
        CancellationToken cancellationToken
    )
    {
        var agent = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == request.OrganizationId
                    && candidate.Id == request.AgentId,
                cancellationToken
            );
        if (agent is null)
            return null;
        var relatedIds = new[] { agent.SponsorAgentId, agent.PlacementParentAgentId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();
        var relatedCodes = await db
            .Agents.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && relatedIds.Contains(candidate.Id)
            )
            .ToDictionaryAsync(
                candidate => candidate.Id,
                candidate => candidate.AgentCode,
                cancellationToken
            );
        var children = await db
            .Agents.AsNoTracking()
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                && candidate.PlacementParentAgentId == agent.Id
            )
            .Select(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        var descendants = await db.PlacementClosures.CountAsync(
            closure =>
                closure.OrganizationId == request.OrganizationId
                && closure.AncestorAgentId == agent.Id,
            cancellationToken
        );
        var paidOrders = await db.Orders.CountAsync(
            order =>
                order.OrganizationId == request.OrganizationId
                && order.AttributedAgentId == agent.Id
                && (
                    order.PaymentStatus == PaymentStatus.Paid
                    || order.PaymentStatus == PaymentStatus.PartiallyRefunded
                    || order.PaymentStatus == PaymentStatus.Refunded
                ),
            cancellationToken
        );
        var volumeEntries = await db.BinaryVolumeEntries.CountAsync(
            entry =>
                entry.OrganizationId == request.OrganizationId && entry.SourceAgentId == agent.Id,
            cancellationToken
        );
        var commissions = await db.CommissionTransactions.CountAsync(
            entry =>
                entry.OrganizationId == request.OrganizationId
                && (entry.BeneficiaryAgentId == agent.Id || entry.SourceAgentId == agent.Id),
            cancellationToken
        );
        var eligibility = movePolicy.Evaluate(
            new PlacementCommitmentSnapshot(descendants, paidOrders, volumeEntries, commissions)
        );
        var identities = await identityReader.GetByUserIdsAsync([agent.UserId], cancellationToken);
        identities.TryGetValue(agent.UserId, out var identity);
        string? GetCode(Guid? id) =>
            id.HasValue && relatedCodes.TryGetValue(id.Value, out var code) ? code : null;
        var placement = new AgentPlacementStatusDto(
            agent.PlacementParentAgentId.HasValue,
            agent.PlacementParentAgentId,
            GetCode(agent.PlacementParentAgentId),
            agent.PlacementSide,
            agent.PreferredLeg,
            children,
            descendants,
            paidOrders + volumeEntries + commissions > 0,
            agent.PlacementParentAgentId.HasValue && eligibility.IsEligible,
            eligibility.ReasonCode,
            eligibility.Reason
        );
        return new AdminAgentDetailsDto(
            agent.Id,
            agent.AgentCode,
            agent.ReferralCode,
            identity?.DisplayName ?? agent.AgentCode,
            identity?.Email ?? string.Empty,
            identity?.EmailConfirmed ?? false,
            agent.Status,
            agent.SponsorAgentId,
            GetCode(agent.SponsorAgentId),
            agent.JoinedAt,
            agent.ActivatedAt,
            agent.QualificationState,
            placement,
            agent.Status is AgentStatus.Applied or AgentStatus.PendingApproval,
            agent.Status
                is AgentStatus.Applied
                    or AgentStatus.PendingApproval
                    or AgentStatus.Inactive,
            agent.Status == AgentStatus.Active,
            agent.Status == AgentStatus.Suspended
        );
    }
}
