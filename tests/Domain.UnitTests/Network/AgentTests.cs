using modular_mlm.Domain.Network;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Network;

public sealed class AgentTests
{
    [Test]
    public void AgentCannotBePlacedTwice()
    {
        var agent = Agent.Apply(Guid.NewGuid(), "user", "AG-1", "REF1", DateTimeOffset.UtcNow);
        agent.Place(Guid.NewGuid(), PlacementSide.Left);
        Should.Throw<Exception>(() => agent.Place(Guid.NewGuid(), PlacementSide.Right));
    }

    [Test]
    public void SubmittedApplicationCanBeActivated()
    {
        var agent = CreateAgent();
        agent.SubmitForApproval();

        agent.Activate(DateTimeOffset.UtcNow);

        agent.Status.ShouldBe(AgentStatus.Active);
        agent.ActivatedAt.ShouldNotBeNull();
    }

    [Test]
    public void PendingApplicationCanBeRejected()
    {
        var agent = CreateAgent();
        agent.SubmitForApproval();

        agent.RejectApplication();

        agent.Status.ShouldBe(AgentStatus.Closed);
    }

    [Test]
    public void ActiveAgentCanBeSuspendedAndReactivated()
    {
        var agent = CreateAgent();
        agent.Activate(DateTimeOffset.UtcNow);

        agent.Suspend();
        agent.Status.ShouldBe(AgentStatus.Suspended);

        agent.Reactivate();
        agent.Status.ShouldBe(AgentStatus.Active);
    }

    [Test]
    public void NonActiveAgentCannotBeSuspended()
    {
        var agent = CreateAgent();

        Should.Throw<Exception>(agent.Suspend);
    }

    [Test]
    public void ActiveAgentCanRegenerateReferralCode()
    {
        var agent = CreateAgent();
        agent.Activate(DateTimeOffset.UtcNow);

        agent.RegenerateReferralCode("new-code");

        agent.ReferralCode.ShouldBe("NEW-CODE");
    }

    private static Agent CreateAgent() =>
        Agent.Apply(Guid.NewGuid(), "user", "AG-1", "REF1", DateTimeOffset.UtcNow);
}
