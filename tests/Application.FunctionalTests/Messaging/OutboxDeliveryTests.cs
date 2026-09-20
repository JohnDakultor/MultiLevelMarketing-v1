using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Events;
using modular_mlm.Domain.Organizations;
using modular_mlm.Infrastructure.Data;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Application.FunctionalTests.Messaging;

using static Infrastructure.TestApp;

public sealed class OutboxDeliveryTests : TestBase
{
    [Test]
    public async Task ShouldCommitAndProcessADomainEventExactlyOnce()
    {
        var organization = Organization.Create("Outbox Test", $"outbox-{Guid.NewGuid():N}", "PHP");
        var paidAt = DateTimeOffset.UtcNow;
        var plan = CommissionPlan.Draft(
            organization.Id,
            "Outbox Compensation Plan",
            1,
            paidAt.AddDays(-1)
        );
        plan.ConfigureDirectSales(0.10m);
        plan.Publish();
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            null,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Outbox Product",
            "OUTBOX-SKU",
            100m,
            1,
            100m,
            null,
            10m,
            null
        );
        order.MarkPaid(paidAt);
        order.ClearDomainEvents();
        organization.AddDomainEvent(new OrderPaidEvent(organization.Id, order.Id));

        await ExecuteInScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<ApplicationDbContext>();
            db.AddRange(organization, plan, order);
            return await db.SaveChangesAsync();
        });
        var messages = await ExecuteInScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<ApplicationDbContext>();
            return await db.OutboxMessages.AsNoTracking().ToListAsync();
        });
        messages
            .Select(candidate => candidate.MessageType)
            .ShouldContain(candidate => candidate.EndsWith(nameof(OrderPaidEvent)));
        var message = messages.Single(candidate =>
            candidate.MessageType.EndsWith(nameof(OrderPaidEvent))
        );

        await Task.WhenAll(DispatchAsync(), DispatchAsync());

        var processedMessage = await FindAsync<ProcessedMessage>(message.Id, "MediatRDomainEvents");
        processedMessage.ShouldNotBeNull();
        (
            await CountAsync<ProcessedMessage>(candidate => candidate.MessageId == message.Id)
        ).ShouldBe(1);
        (await FindAsync<OutboxMessage>(message.Id))!.ProcessedAt.ShouldNotBeNull();
    }

    private static Task<int> DispatchAsync() =>
        ExecuteInScopeAsync(provider =>
            provider
                .GetRequiredService<OutboxDispatcher>()
                .DispatchBatchAsync(10, CancellationToken.None)
        );
}
