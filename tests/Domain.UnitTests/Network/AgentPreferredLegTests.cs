using modular_mlm.Domain.Events;
using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Network;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class AgentPreferredLegTests
{
    [TestCase(PlacementSide.Left)]
    [TestCase(PlacementSide.Right)]
    public void AgentCanSetPreferredLeg(PlacementSide side)
    {
        var organizationId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.Parse("2026-09-12T10:00:00Z");
        var agent = Agent.Apply(organizationId, "user", "AG-1", "REF-1", occurredAt);

        agent.SetPreferredLeg(side, occurredAt).ShouldBeTrue();

        agent.PreferredLeg.ShouldBe(side);
        var raised = agent.DomainEvents.OfType<AgentPreferredLegChangedEvent>().Single();
        raised.OrganizationId.ShouldBe(organizationId);
        raised.AgentId.ShouldBe(agent.Id);
        raised.PreferredLeg.ShouldBe(side);
        raised.OccurredAt.ShouldBe(occurredAt);
    }

    [Test]
    public void RepeatingPreferenceIsIdempotent()
    {
        var agent = CreateAgent();
        agent.SetPreferredLeg(PlacementSide.Left, DateTimeOffset.UtcNow);
        agent.ClearDomainEvents();

        agent.SetPreferredLeg(PlacementSide.Left, DateTimeOffset.UtcNow).ShouldBeFalse();

        agent.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void SuspendedAgentCannotChangePreference()
    {
        var agent = CreateAgent();
        agent.Activate(DateTimeOffset.UtcNow);
        agent.Suspend();

        Should.Throw<DomainInvariantException>(() =>
            agent.SetPreferredLeg(PlacementSide.Right, DateTimeOffset.UtcNow)
        );
    }

    [Test]
    public void UndefinedPreferenceIsRejected()
    {
        var agent = CreateAgent();

        Should.Throw<DomainInvariantException>(() =>
            agent.SetPreferredLeg((PlacementSide)999, DateTimeOffset.UtcNow)
        );
    }

    private static Agent CreateAgent() =>
        Agent.Apply(Guid.NewGuid(), "user", "AG-1", "REF-1", DateTimeOffset.UtcNow);
}
