using modular_mlm.Application.Payouts.Commands.RequestPayout;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Payouts;

using static Infrastructure.TestApp;

public sealed class RequestPayoutTests : TestBase
{
    [Test]
    public async Task EligibleRequestCreatesPayoutAndReservesFundsAtomically()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m);

        var payoutId = await SendAsync(CreateCommand(data, amount: 200m));

        var payout = await FindAsync<PayoutRequest>(payoutId);
        payout.ShouldNotBeNull();
        payout.OrganizationId.ShouldBe(data.OrganizationId);
        payout.AgentId.ShouldBe(data.AgentId);
        payout.PayoutAccountId.ShouldBe(data.AccountId);
        payout.Amount.ShouldBe(200m);
        payout.Currency.ShouldBe("PHP");
        payout.Status.ShouldBe(PayoutStatus.UnderReview);

        var hold = await SingleAsync<WalletEntry>(entry =>
            entry.WalletId == data.WalletId
            && entry.Type == WalletEntryType.Hold
            && entry.SourceType == "PayoutRequest"
            && entry.SourceId == payoutId
        );
        hold.Amount.ShouldBe(-200m);
    }

    [Test]
    public async Task AmountBelowOrganizationMinimumIsRejectedWithoutReservation()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m, minimumPayout: 100m);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data, amount: 99.99m))
        );

        exception.Message.ShouldContain("at least 100");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
        (await CountAsync<WalletEntry>(entry => entry.Type == WalletEntryType.Hold)).ShouldBe(0);
    }

    [Test]
    public async Task CurrencyMismatchIsRejected()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data, currency: "USD"))
        );

        exception.Message.ShouldContain("currency");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    [Test]
    public async Task UnverifiedPayoutAccountIsRejected()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m, verifyAccount: false);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data))
        );

        exception.Message.ShouldContain("verified payout account");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    [Test]
    public async Task SuspendedAgentIsRejected()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m, suspendAgent: true);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data))
        );

        exception.Message.ShouldContain("active agent");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    [Test]
    public async Task HeldWalletIsRejected()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m, holdWallet: true);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data))
        );

        exception.Message.ShouldContain("active agent wallet");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    [Test]
    public async Task ExistingReservationReducesAvailableFundsForNextRequest()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 500m);

        await SendAsync(CreateCommand(data, amount: 450m));
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data, amount: 100m))
        );

        exception.Message.ShouldContain("available wallet balance");
        (await CountAsync<PayoutRequest>()).ShouldBe(1);
        (await CountAsync<WalletEntry>(entry => entry.Type == WalletEntryType.Hold)).ShouldBe(1);
    }

    [Test]
    public async Task RecoverableNegativeBalanceIsRejectedWhenDisabled()
    {
        var data = await CreatePayoutScenarioAsync(availableBalance: 0m);
        await AddAsync(
            WalletEntry.Create(
                data.WalletId,
                WalletEntryType.Debit,
                -50m,
                "Recovery",
                Guid.NewGuid()
            )
        );

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(CreateCommand(data, amount: 10m))
        );

        exception.Message.ShouldContain("negative balance");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    private static RequestPayoutCommand CreateCommand(
        PayoutScenario data,
        decimal amount = 100m,
        string currency = "PHP"
    ) => new(data.OrganizationId, data.AgentId, data.AccountId, amount, currency);

    private static async Task<PayoutScenario> CreatePayoutScenarioAsync(
        decimal availableBalance,
        decimal minimumPayout = 10m,
        bool verifyAccount = true,
        bool suspendAgent = false,
        bool holdWallet = false
    )
    {
        var organization = Organization.Create(
            "Payout Request Test",
            $"payout-request-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.Wallet.Update(
            organization.Wallet.CommissionReleaseTrigger,
            0,
            0,
            minimumPayout,
            false,
            0m
        );
        await AddAsync(organization);

        var userId = await RunAsUserAsync(
            $"payout-agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var now = DateTimeOffset.UtcNow;
        var agent = Agent.Apply(
            organization.Id,
            userId,
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            now
        );
        agent.Activate(now);
        if (suspendAgent)
            agent.Suspend();

        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        if (holdWallet)
            wallet.Hold();

        var account = PayoutAccount.Register(
            organization.Id,
            agent.Id,
            "bank",
            "****1234",
            "Protected Name",
            "Protected Number",
            "BNORPHMM",
            "instapay"
        );
        if (verifyAccount)
            account.Verify();

        await AddAsync(agent);
        await AddAsync(wallet);
        await AddAsync(account);
        if (availableBalance > 0m)
        {
            await AddAsync(
                WalletEntry.Create(
                    wallet.Id,
                    WalletEntryType.AvailableCredit,
                    availableBalance,
                    "PayoutTestCredit",
                    Guid.NewGuid()
                )
            );
        }

        return new PayoutScenario(organization.Id, agent.Id, wallet.Id, account.Id);
    }

    private sealed record PayoutScenario(
        Guid OrganizationId,
        Guid AgentId,
        Guid WalletId,
        Guid AccountId
    );
}
