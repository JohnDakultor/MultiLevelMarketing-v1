using modular_mlm.Application.Wallets.Commands.ReleasePendingCommissions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Wallets;

using static Infrastructure.TestApp;

public sealed class ReleasePendingCommissionsTests : TestBase
{
    [Test]
    public async Task EligibleCommissionIsReleasedExactlyOnce()
    {
        var data = await CreatePendingCommissionAsync(releaseDelayDays: 0);
        var command = new ReleasePendingCommissionsCommand(
            data.OrganizationId,
            100,
            data.PaidAt.AddMinutes(1)
        );

        var first = await SendAsync(command);
        var second = await SendAsync(command);

        first.ReleasedCount.ShouldBe(1);
        second.ReleasedCount.ShouldBe(0);
        var commission = await FindAsync<CommissionTransaction>(data.CommissionId);
        commission!.Status.ShouldBe(CommissionStatus.Available);
        (
            await CountAsync<WalletEntry>(entry => entry.ReleasedFromEntryId == data.PendingEntryId)
        ).ShouldBe(1);
    }

    [Test]
    public async Task CommissionRemainsPendingUntilReleaseDelayElapses()
    {
        var data = await CreatePendingCommissionAsync(releaseDelayDays: 2);

        var result = await SendAsync(
            new ReleasePendingCommissionsCommand(data.OrganizationId, 100, data.PaidAt.AddDays(1))
        );

        result.ReleasedCount.ShouldBe(0);
        result.IneligibleCount.ShouldBe(1);
        var commission = await FindAsync<CommissionTransaction>(data.CommissionId);
        commission!.Status.ShouldBe(CommissionStatus.Pending);
        (
            await CountAsync<WalletEntry>(entry => entry.ReleasedFromEntryId == data.PendingEntryId)
        ).ShouldBe(0);
    }

    [Test]
    public async Task ConcurrentReleaseCommandsCreateOnlyOneAvailableCredit()
    {
        var data = await CreatePendingCommissionAsync(releaseDelayDays: 0);
        var command = new ReleasePendingCommissionsCommand(
            data.OrganizationId,
            100,
            data.PaidAt.AddMinutes(1)
        );

        var results = await Task.WhenAll(SendAsync(command), SendAsync(command));

        results.Sum(result => result.ReleasedCount).ShouldBe(1);
        (
            await CountAsync<WalletEntry>(entry => entry.ReleasedFromEntryId == data.PendingEntryId)
        ).ShouldBe(1);
    }

    internal static async Task<PendingCommissionData> CreatePendingCommissionAsync(
        int releaseDelayDays
    )
    {
        var paidAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var organization = Organization.Create(
            "Wallet Release Test",
            $"wallet-release-{Guid.NewGuid():N}",
            "PHP",
            "Asia/Manila",
            "en-PH"
        );
        organization.Wallet.Update(
            organization.Wallet.CommissionReleaseTrigger,
            releaseDelayDays,
            0,
            10m,
            false,
            0m
        );
        var agent = Agent.Apply(
            organization.Id,
            Guid.NewGuid().ToString(),
            $"AG-{Guid.NewGuid():N}",
            $"REF-{Guid.NewGuid():N}",
            paidAt.AddDays(-1)
        );
        agent.Activate(paidAt.AddDays(-1));
        var plan = CommissionPlan.Draft(
            organization.Id,
            "Wallet Release Plan",
            1,
            paidAt.AddDays(-1)
        );
        plan.ConfigureDirectSales(0.10m);
        plan.Publish();
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            agent.Id,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Wallet Product",
            $"SKU-{Guid.NewGuid():N}",
            1_000m,
            1,
            1_000m,
            null,
            100m,
            null
        );
        order.MarkPaid(paidAt);
        var orderItem = order.Items.Single();
        var commission = CommissionTransaction.CreateDirectSale(
            organization.Id,
            agent.Id,
            order.Id,
            orderItem.Id,
            plan.Id,
            "direct-sale:v1",
            1_000m,
            0.10m,
            100m
        );
        var wallet = AgentWallet.Open(organization.Id, agent.Id, "PHP");
        var pendingEntry = WalletEntry.Create(
            wallet.Id,
            WalletEntryType.PendingCredit,
            commission.Amount,
            nameof(CommissionTransaction),
            commission.Id
        );

        await AddAsync(organization);
        await AddAsync(agent);
        await AddAsync(plan);
        await AddAsync(order);
        await AddAsync(wallet);
        await AddAsync(commission);
        await AddAsync(pendingEntry);

        return new PendingCommissionData(organization.Id, commission.Id, pendingEntry.Id, paidAt);
    }

    internal sealed record PendingCommissionData(
        Guid OrganizationId,
        Guid CommissionId,
        Guid PendingEntryId,
        DateTimeOffset PaidAt
    );
}
