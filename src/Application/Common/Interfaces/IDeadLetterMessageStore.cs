using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IDeadLetterMessageStore
{
    Task<DeadLetterMessagePage> GetAsync(
        DeadLetterMessageQuery query,
        CancellationToken cancellationToken
    );

    Task<DeadLetterReplayResult> ReplayAsync(
        DeadLetterReplayRequest request,
        CancellationToken cancellationToken
    );
}
