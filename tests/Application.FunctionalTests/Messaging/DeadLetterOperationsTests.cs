using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Application.FunctionalTests.Messaging;

using static Infrastructure.TestApp;

public sealed class DeadLetterOperationsTests : TestBase
{
    [Test]
    public async Task TenantScopedMessageCanBeListedAndRequeuedWithoutPayloadDisclosure()
    {
        var organization = Organization.Create(
            "Dead Letter",
            $"dead-letter-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);
        var actor = Guid.NewGuid();
        var messageId = await ExecuteInScopeAsync(async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var now = DateTimeOffset.UtcNow;
            var message = OutboxMessage.Create(
                new NotificationReadEvent(organization.Id, Guid.NewGuid(), Guid.NewGuid(), now),
                now,
                null
            );
            message.MarkFailed("sensitive provider detail", now, 1, TimeSpan.Zero);
            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
            return message.Id;
        });

        await ExecuteInScopeAsync(async services =>
        {
            var store = services.GetRequiredService<IDeadLetterMessageStore>();
            var page = await store.GetAsync(
                new DeadLetterMessageQuery(organization.Id, 1, 20, null, null, null),
                CancellationToken.None
            );
            page.Items.Single().LastErrorSummary.ShouldBe("Processing failed.");
            var replay = await store.ReplayAsync(
                new DeadLetterReplayRequest(
                    organization.Id,
                    messageId,
                    1,
                    actor.ToString(),
                    "Operator retry",
                    DateTimeOffset.UtcNow
                ),
                CancellationToken.None
            );
            replay.MessageId.ShouldBe(messageId);
            return true;
        });
    }
}
