using MediatR;
using Microsoft.Extensions.Logging;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxPublisher(IPublisher publisher, ILogger<OutboxPublisher> logger)
    : IOutboxPublisher
{
    public Task PublishAsync(object message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Dispatching outbox message {MessageType}", message.GetType().Name);
        return publisher.Publish(message, cancellationToken);
    }
}
