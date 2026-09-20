using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Application.Payouts.Commands.ApprovePayout;
using modular_mlm.Application.Payouts.Commands.ProcessPayout;
using modular_mlm.Application.Payouts.Commands.ReconcilePayout;
using modular_mlm.Application.Payouts.Commands.RequestPayout;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payouts;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Payouts;

using static Infrastructure.TestApp;

public sealed class PayoutAdministrationTests : TestBase
{
    [Test]
    public async Task ShouldRejectPayoutBelowOrganizationMinimum()
    {
        var organization = Organization.Create(
            "Minimum Payout Test",
            $"minimum-payout-{Guid.NewGuid():N}",
            "PHP"
        );
        organization.Wallet.Update(
            organization.Wallet.CommissionReleaseTrigger,
            0,
            0,
            100m,
            false,
            0m
        );
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"minimum-agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var agent = Agent.Apply(
            organization.Id,
            userId,
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        var account = PayoutAccount.Register(
            organization.Id,
            agent.Id,
            "bank",
            "****4567",
            "Juan Dela Cruz",
            "1234567",
            "BNORPHMM",
            "instapay"
        );
        account.Verify();
        await AddAsync(agent);
        await AddAsync(wallet);
        await AddAsync(
            WalletEntry.Create(
                wallet.Id,
                WalletEntryType.AvailableCredit,
                1_000m,
                "TestCredit",
                Guid.NewGuid()
            )
        );
        await AddAsync(account);

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            SendAsync(new RequestPayoutCommand(organization.Id, agent.Id, account.Id, 50m, "PHP"))
        );

        exception.Message.ShouldContain("at least 100");
        (await CountAsync<PayoutRequest>()).ShouldBe(0);
    }

    [Test]
    public async Task ShouldHoldSubmitAndReconcileAPayMongoPayout()
    {
        var organization = Organization.Create("Payout Test", $"payout-{Guid.NewGuid():N}", "PHP");
        await AddAsync(organization);
        var userId = await RunAsUserAsync(
            $"agent-{Guid.NewGuid():N}@local",
            "Testing1234!",
            ["Agent"]
        );
        var agent = Agent.Apply(
            organization.Id,
            userId,
            $"AG{Guid.NewGuid():N}"[..12],
            $"RF{Guid.NewGuid():N}"[..12],
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        var account = PayoutAccount.Register(
            organization.Id,
            agent.Id,
            "bank",
            "****4567",
            "Juan Dela Cruz",
            "1234567",
            "BNORPHMM",
            "instapay"
        );
        account.Verify();
        await AddAsync(agent);
        await AddAsync(wallet);
        await AddAsync(
            WalletEntry.Create(
                wallet.Id,
                WalletEntryType.AvailableCredit,
                1_000m,
                "TestCredit",
                Guid.NewGuid()
            )
        );
        await AddAsync(account);

        var payoutId = await SendAsync(
            new RequestPayoutCommand(organization.Id, agent.Id, account.Id, 500m, "PHP")
        );
        (await FindAsync<PayoutRequest>(payoutId))!.Status.ShouldBe(PayoutStatus.UnderReview);

        await RunAsAdministratorAsync(organization.Id);
        await SendAsync(new ApprovePayoutCommand(organization.Id, payoutId));
        var transferId = await SendAsync(new ProcessPayoutCommand(organization.Id, payoutId));
        transferId.ShouldBe("transfer_test");
        (await FindAsync<PayoutRequest>(payoutId))!.Status.ShouldBe(PayoutStatus.Processing);

        GetRequiredService<TestPayoutProvider>().StatusResult = new ProviderPayoutResult(
            "batch_test",
            "transfer_test",
            "succeeded",
            "provider_ref",
            null,
            null
        );
        (await SendAsync(new ReconcilePayoutCommand(payoutId))).ShouldBeTrue();

        var paid = await FindAsync<PayoutRequest>(payoutId);
        paid!.Status.ShouldBe(PayoutStatus.Paid);
        paid.ProviderReference.ShouldBe("provider_ref");
        foreach (
            var definition in new[]
            {
                AuditCoverageMap.PayoutApproved,
                AuditCoverageMap.PayoutProcessingStarted,
                AuditCoverageMap.PayoutCompleted,
            }
        )
        {
            var audit = await SingleAsync<AuditLog>(entry =>
                entry.OrganizationId == organization.Id
                && entry.EntityId == payoutId
                && entry.Action == definition.Action
            );
            audit.EntityType.ShouldBe(definition.EntityType);
            audit.BeforeJson.ShouldNotBe(audit.AfterJson);
        }
    }
}
