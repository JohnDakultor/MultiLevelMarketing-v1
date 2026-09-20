using modular_mlm.Domain.Exceptions;
using modular_mlm.Domain.Wallets;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Wallets;

public sealed class AgentWalletTests
{
    [Test]
    public void OpenNormalizesCurrencyAndStartsActive()
    {
        var organizationId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var wallet = AgentWallet.Open(organizationId, agentId, "php");

        wallet.OrganizationId.ShouldBe(organizationId);
        wallet.AgentId.ShouldBe(agentId);
        wallet.Currency.ShouldBe("PHP");
        wallet.Status.ShouldBe(WalletStatus.Active);
    }

    [TestCaseSource(nameof(InvalidOpenArguments))]
    public void OpenRejectsInvalidIdentityOrCurrency(
        Guid organizationId,
        Guid agentId,
        string currency
    )
    {
        Should.Throw<DomainInvariantException>(() =>
            AgentWallet.Open(organizationId, agentId, currency)
        );
    }

    [Test]
    public void HoldMovesActiveWalletToHeld()
    {
        var wallet = OpenWallet();

        wallet.Hold();

        wallet.Status.ShouldBe(WalletStatus.Held);
    }

    [Test]
    public void ReactivateMovesHeldWalletToActive()
    {
        var wallet = OpenWallet();
        wallet.Hold();

        wallet.Reactivate();

        wallet.Status.ShouldBe(WalletStatus.Active);
    }

    [Test]
    public void ReactivateRejectsClosedWallet()
    {
        var wallet = OpenWallet();
        wallet.Close();

        var exception = Should.Throw<DomainInvariantException>(wallet.Reactivate);

        exception.Message.ShouldContain("Closed wallet");
        wallet.Status.ShouldBe(WalletStatus.Closed);
    }

    [Test]
    public void CloseLeavesWalletInTerminalState()
    {
        var wallet = OpenWallet();

        wallet.Close();
        var exception = Should.Throw<DomainInvariantException>(wallet.Reactivate);

        wallet.Status.ShouldBe(WalletStatus.Closed);
        exception.Message.ShouldContain("cannot be reactivated");
    }

    private static AgentWallet OpenWallet() =>
        AgentWallet.Open(Guid.NewGuid(), Guid.NewGuid(), "PHP");

    private static IEnumerable<TestCaseData> InvalidOpenArguments()
    {
        yield return new TestCaseData(Guid.Empty, Guid.NewGuid(), "PHP").SetName(
            "Open_requires_an_organization"
        );
        yield return new TestCaseData(Guid.NewGuid(), Guid.Empty, "PHP").SetName(
            "Open_requires_an_agent"
        );
        yield return new TestCaseData(Guid.NewGuid(), Guid.NewGuid(), "PH").SetName(
            "Open_rejects_short_currency"
        );
        yield return new TestCaseData(Guid.NewGuid(), Guid.NewGuid(), "PHPP").SetName(
            "Open_rejects_long_currency"
        );
    }
}
