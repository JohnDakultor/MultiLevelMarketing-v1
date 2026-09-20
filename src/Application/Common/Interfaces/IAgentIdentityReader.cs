using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IAgentIdentityReader
{
    Task<IReadOnlyDictionary<string, AgentIdentitySummary>> GetByUserIdsAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken
    );
}
