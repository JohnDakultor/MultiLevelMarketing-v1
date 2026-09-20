using modular_mlm.Domain.Events;
using modular_mlm.Infrastructure.Messaging;

namespace modular_mlm.Application.FunctionalTests.Messaging;

public sealed class OutboxConsumerTests
{
    [TestCase(typeof(OrderPaidEvent))]
    [TestCase(typeof(PaymentFailedEvent))]
    [TestCase(typeof(OrderItemRefundedEvent))]
    [TestCase(typeof(CommissionReleasedEvent))]
    [TestCase(typeof(PayoutApprovedEvent))]
    [TestCase(typeof(AdministratorInvitedEvent))]
    public void CrossModuleEventIsExplicitlyRegistered(Type eventType)
    {
        var registry = new OutboxConsumerRegistry();
        registry.IsAllowed(OutboxConsumerRegistry.StableName(eventType)).ShouldBeTrue();
    }
}
