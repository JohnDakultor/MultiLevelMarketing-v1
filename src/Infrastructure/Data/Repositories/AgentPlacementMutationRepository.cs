using System.Data;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;
using Npgsql;

namespace modular_mlm.Infrastructure.Data.Repositories;

public sealed class AgentPlacementMutationRepository(
    ApplicationDbContext db,
    AgentPlacementRepository placementRepository
) : IAgentPlacementMutationRepository
{
    public Task<bool> TryPlaceAsync(
        Guid organizationId,
        Guid agentId,
        PlacementDecision decision,
        Action<Agent> onPlaced,
        CancellationToken cancellationToken
    ) =>
        placementRepository.TryPlaceAsync(
            organizationId,
            agentId,
            decision,
            onPlaced,
            cancellationToken
        );

    public Task<bool> TryMoveUncommittedAsync(
        Guid organizationId,
        Guid agentId,
        Guid newParentAgentId,
        PlacementSide newSide,
        Guid expectedCurrentParentAgentId,
        PlacementSide expectedCurrentSide,
        string actorUserId,
        string reason,
        DateTimeOffset occurredAt,
        Action<Agent> onMoved,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(onMoved);
        return db
            .Database.CreateExecutionStrategy()
            .ExecuteAsync(() =>
                MoveWithinTransactionAsync(
                    organizationId,
                    agentId,
                    newParentAgentId,
                    newSide,
                    expectedCurrentParentAgentId,
                    expectedCurrentSide,
                    actorUserId,
                    reason,
                    occurredAt,
                    onMoved,
                    cancellationToken
                )
            );
    }

    private async Task<bool> MoveWithinTransactionAsync(
        Guid organizationId,
        Guid agentId,
        Guid newParentAgentId,
        PlacementSide newSide,
        Guid expectedCurrentParentAgentId,
        PlacementSide expectedCurrentSide,
        string actorUserId,
        string reason,
        DateTimeOffset occurredAt,
        Action<Agent> onMoved,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken
        );
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                SELECT 1 FROM "Agents"
                WHERE "OrganizationId" = {organizationId}
                  AND "Id" IN ({agentId}, {expectedCurrentParentAgentId}, {newParentAgentId})
                ORDER BY "Id" FOR UPDATE
                """,
                cancellationToken
            );
            var agent = await db.Agents.SingleOrDefaultAsync(
                candidate => candidate.OrganizationId == organizationId && candidate.Id == agentId,
                cancellationToken
            );
            var parent = await db.Agents.SingleOrDefaultAsync(
                candidate =>
                    candidate.OrganizationId == organizationId && candidate.Id == newParentAgentId,
                cancellationToken
            );
            if (agent is null || parent is null)
                throw new KeyNotFoundException("Agent or placement parent was not found.");
            if (parent.Status != AgentStatus.Active)
                throw new InvalidOperationException("Placement parent must be active.");
            if (
                agent.PlacementParentAgentId != expectedCurrentParentAgentId
                || agent.PlacementSide != expectedCurrentSide
            )
                return await RollbackConflictAsync(transaction, cancellationToken);
            var occupied = await db.Agents.AnyAsync(
                candidate =>
                    candidate.OrganizationId == organizationId
                    && candidate.Id != agentId
                    && candidate.PlacementParentAgentId == newParentAgentId
                    && candidate.PlacementSide == newSide,
                cancellationToken
            );
            var cycle = await db.PlacementClosures.AnyAsync(
                closure =>
                    closure.OrganizationId == organizationId
                    && closure.AncestorAgentId == agentId
                    && closure.DescendantAgentId == newParentAgentId,
                cancellationToken
            );
            var descendants = await db.PlacementClosures.AnyAsync(
                closure =>
                    closure.OrganizationId == organizationId && closure.AncestorAgentId == agentId,
                cancellationToken
            );
            var paidSales = await db.Orders.AnyAsync(
                order =>
                    order.OrganizationId == organizationId
                    && order.AttributedAgentId == agentId
                    && (
                        order.PaymentStatus == PaymentStatus.Paid
                        || order.PaymentStatus == PaymentStatus.PartiallyRefunded
                        || order.PaymentStatus == PaymentStatus.Refunded
                    ),
                cancellationToken
            );
            var volume = await db.BinaryVolumeEntries.AnyAsync(
                entry => entry.OrganizationId == organizationId && entry.SourceAgentId == agentId,
                cancellationToken
            );
            var commissions = await db.CommissionTransactions.AnyAsync(
                entry =>
                    entry.OrganizationId == organizationId
                    && (entry.BeneficiaryAgentId == agentId || entry.SourceAgentId == agentId),
                cancellationToken
            );
            if (occupied || cycle || descendants || paidSales || volume || commissions)
                return await RollbackConflictAsync(transaction, cancellationToken);
            var oldClosures = await db
                .PlacementClosures.Where(closure =>
                    closure.OrganizationId == organizationId && closure.DescendantAgentId == agentId
                )
                .ToListAsync(cancellationToken);
            db.PlacementClosures.RemoveRange(oldClosures);
            agent.MovePlacement(newParentAgentId, newSide, actorUserId, reason, occurredAt);
            db.PlacementClosures.Add(
                PlacementClosure.Create(organizationId, newParentAgentId, agentId, 1, newSide)
            );
            var ancestors = await db
                .PlacementClosures.AsNoTracking()
                .Where(closure =>
                    closure.OrganizationId == organizationId
                    && closure.DescendantAgentId == newParentAgentId
                )
                .ToListAsync(cancellationToken);
            db.PlacementClosures.AddRange(
                ancestors.Select(ancestor =>
                    PlacementClosure.Create(
                        organizationId,
                        ancestor.AncestorAgentId,
                        agentId,
                        ancestor.Depth + 1,
                        ancestor.FirstLeg
                    )
                )
            );
            onMoved(agent);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception) when (IsConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgres && IsConflict(postgres))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task<bool> RollbackConflictAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        await transaction.RollbackAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return false;
    }

    private static bool IsConflict(PostgresException exception) =>
        exception.SqlState
            is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.SerializationFailure
                or PostgresErrorCodes.DeadlockDetected;
}
