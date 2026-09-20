using System.Data;
using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.ValueObjects;
using Npgsql;

namespace modular_mlm.Infrastructure.Data.Repositories;

public sealed class AgentPlacementRepository(ApplicationDbContext db) : IAgentPlacementRepository
{
    public async Task<IReadOnlyCollection<PlacementCandidate>> GetCandidatesAsync(
        Guid organizationId,
        Guid rootAgentId,
        int maxDepth,
        CancellationToken cancellationToken
    )
    {
        var root = await db
            .Agents.AsNoTracking()
            .SingleOrDefaultAsync(
                agent => agent.Id == rootAgentId && agent.OrganizationId == organizationId,
                cancellationToken
            );
        if (root is null)
            throw new InvalidOperationException(
                "Placement root was not found in this organization."
            );
        if (root.Status != AgentStatus.Active)
            throw new InvalidOperationException("Placement root must be an active agent.");

        var descendantIds = await db
            .PlacementClosures.AsNoTracking()
            .Where(closure =>
                closure.OrganizationId == organizationId
                && closure.AncestorAgentId == rootAgentId
                && closure.Depth <= maxDepth
            )
            .Select(closure => closure.DescendantAgentId)
            .ToListAsync(cancellationToken);

        var networkIds = descendantIds.Append(rootAgentId).Distinct().ToArray();
        var agents = await db
            .Agents.AsNoTracking()
            .Where(agent => agent.OrganizationId == organizationId && networkIds.Contains(agent.Id))
            .ToListAsync(cancellationToken);
        var agentsById = agents.ToDictionary(agent => agent.Id);

        var closures = await db
            .PlacementClosures.AsNoTracking()
            .Where(closure =>
                closure.OrganizationId == organizationId
                && networkIds.Contains(closure.AncestorAgentId)
            )
            .ToListAsync(cancellationToken);

        var children = agents
            .Where(agent => agent.PlacementParentAgentId is not null)
            .GroupBy(agent => agent.PlacementParentAgentId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(agent => agent.PlacementSide!.Value)
            );
        var legSizes = closures
            .GroupBy(closure => new { closure.AncestorAgentId, closure.FirstLeg })
            .ToDictionary(group => group.Key, group => group.Count());

        var candidates = new List<PlacementCandidate>();
        var queue = new Queue<(Guid AgentId, int Depth)>();
        queue.Enqueue((rootAgentId, 0));
        long traversalOrder = 0;

        while (queue.TryDequeue(out var current))
        {
            if (current.Depth > maxDepth)
                continue;
            if (!agentsById.TryGetValue(current.AgentId, out var agent))
                continue;

            children.TryGetValue(agent.Id, out var directChildren);
            var hasLeft = directChildren?.ContainsKey(PlacementSide.Left) == true;
            var hasRight = directChildren?.ContainsKey(PlacementSide.Right) == true;

            if (agent.Status == AgentStatus.Active)
            {
                legSizes.TryGetValue(
                    new { AncestorAgentId = agent.Id, FirstLeg = PlacementSide.Left },
                    out var leftLegSize
                );
                legSizes.TryGetValue(
                    new { AncestorAgentId = agent.Id, FirstLeg = PlacementSide.Right },
                    out var rightLegSize
                );
                candidates.Add(
                    new PlacementCandidate(
                        agent.Id,
                        current.Depth,
                        traversalOrder,
                        hasLeft,
                        hasRight,
                        leftLegSize,
                        rightLegSize
                    )
                );
            }

            traversalOrder++;
            if (hasLeft && current.Depth < maxDepth)
                queue.Enqueue((directChildren![PlacementSide.Left].Id, current.Depth + 1));
            if (hasRight && current.Depth < maxDepth)
                queue.Enqueue((directChildren![PlacementSide.Right].Id, current.Depth + 1));
        }

        return candidates;
    }

    public Task<bool> TryPlaceAsync(
        Guid organizationId,
        Guid agentId,
        PlacementDecision decision,
        Action<Agent> onPlaced,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(onPlaced);
        var strategy = db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() =>
            TryPlaceWithinTransactionAsync(
                organizationId,
                agentId,
                decision,
                onPlaced,
                cancellationToken
            )
        );
    }

    private async Task<bool> TryPlaceWithinTransactionAsync(
        Guid organizationId,
        Guid agentId,
        PlacementDecision decision,
        Action<Agent> onPlaced,
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
                SELECT 1
                FROM "Agents"
                WHERE "OrganizationId" = {organizationId}
                  AND "Id" IN ({agentId}, {decision.ParentAgentId})
                ORDER BY "Id"
                FOR UPDATE
                """,
                cancellationToken
            );

            var agent = await db.Agents.SingleOrDefaultAsync(
                candidate => candidate.Id == agentId && candidate.OrganizationId == organizationId,
                cancellationToken
            );
            var parent = await db.Agents.SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == decision.ParentAgentId
                    && candidate.OrganizationId == organizationId,
                cancellationToken
            );

            if (agent is null || parent is null)
                throw new InvalidOperationException(
                    "Agent or placement parent was not found in this organization."
                );
            if (parent.Status != AgentStatus.Active)
                throw new InvalidOperationException("Placement parent must be active.");
            if (agent.PlacementParentAgentId is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
                return false;
            }

            var slotOccupied = await db.Agents.AnyAsync(
                candidate =>
                    candidate.OrganizationId == organizationId
                    && candidate.PlacementParentAgentId == decision.ParentAgentId
                    && candidate.PlacementSide == decision.Side,
                cancellationToken
            );
            if (slotOccupied)
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();
                return false;
            }

            agent.Place(decision.ParentAgentId, decision.Side);
            db.PlacementClosures.Add(
                PlacementClosure.Create(
                    organizationId,
                    decision.ParentAgentId,
                    agentId,
                    1,
                    decision.Side
                )
            );

            var ancestors = await db
                .PlacementClosures.AsNoTracking()
                .Where(closure =>
                    closure.OrganizationId == organizationId
                    && closure.DescendantAgentId == decision.ParentAgentId
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

            onPlaced(agent);

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (PostgresException exception) when (IsPlacementConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                && IsPlacementConflict(postgresException)
            )
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            return false;
        }
    }

    private static bool IsPlacementConflict(PostgresException exception) =>
        exception.SqlState
            is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.SerializationFailure
                or PostgresErrorCodes.DeadlockDetected;
}
