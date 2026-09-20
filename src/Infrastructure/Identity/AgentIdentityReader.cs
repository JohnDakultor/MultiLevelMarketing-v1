using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.Data;

namespace modular_mlm.Infrastructure.Identity;

public sealed class AgentIdentityReader(ApplicationDbContext db) : IAgentIdentityReader
{
    public async Task<IReadOnlyDictionary<string, AgentIdentitySummary>> GetByUserIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken
    )
    {
        if (userIds.Count == 0)
            return new Dictionary<string, AgentIdentitySummary>();
        var distinctIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return await db
            .Users.AsNoTracking()
            .Where(user => distinctIds.Contains(user.Id))
            .Select(user => new AgentIdentitySummary(
                user.Id,
                string.IsNullOrWhiteSpace(user.DisplayName)
                    ? user.Email ?? string.Empty
                    : user.DisplayName,
                user.Email ?? string.Empty,
                user.EmailConfirmed
            ))
            .ToDictionaryAsync(user => user.UserId, StringComparer.Ordinal, cancellationToken);
    }
}
