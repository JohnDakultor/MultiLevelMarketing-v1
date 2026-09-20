using modular_mlm.Domain.Events;
using modular_mlm.Infrastructure.Messaging;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.Messaging;

public sealed class ConcurrentOutboxDispatcherTests
{
    [Test]
    public void DeadLetterReplayKeepsAttemptHistoryAndClearsClaimState()
    {
        var now = DateTimeOffset.UtcNow;
        var message = OutboxMessage.Create(
            new NotificationReadEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now),
            now,
            null
        );
        message.MarkFailed("failure", now, 1, TimeSpan.Zero).ShouldBeTrue();

        message.Replay(now.AddMinutes(1));

        message.Attempts.ShouldBe(1);
        message.DeadLetteredAt.ShouldBeNull();
        message.NextAttemptAt.ShouldBe(now.AddMinutes(1));
    }
}
