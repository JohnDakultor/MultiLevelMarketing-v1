using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface IBackgroundJobHandler
{
    string JobName { get; }

    Task HandleAsync(DurableJobPayload payload, CancellationToken cancellationToken);
}
