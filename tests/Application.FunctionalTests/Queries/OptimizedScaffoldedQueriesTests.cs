using modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger;
using modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger;
using modular_mlm.Application.Compensation.Queries.GetCommissionPlan;
using modular_mlm.Application.Network.Queries.GetFilteredDownline;
using modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries;
using modular_mlm.Application.Wallets.Queries.GetAdminWallets;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Queries;

using static Infrastructure.TestApp;

public sealed class OptimizedScaffoldedQueriesTests : Infrastructure.TestBase
{
    [Test]
    public async Task AllScaffoldedQueriesExecuteAsServerSideTenantScopedProjections()
    {
        var now = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Optimized Queries",
            $"optimized-queries-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(organization);

        var root = CreateActiveAgent(organization.Id, "ROOT", now.AddDays(-10));
        var child = CreateActiveAgent(organization.Id, "CHILD", now.AddDays(-5), root.Id);
        child.Place(root.Id, PlacementSide.Left);
        await AddAsync(root);
        await AddAsync(child);
        await AddAsync(
            PlacementClosure.Create(
                organization.Id,
                root.Id,
                child.Id,
                depth: 1,
                PlacementSide.Left
            )
        );

        var plan = CommissionPlan.Draft(
            organization.Id,
            "Standard",
            1,
            now.AddDays(-30)
        );
        await AddAsync(plan);
        var orderId = Guid.NewGuid();
        var orderItemId = Guid.NewGuid();
        await AddAsync(
            CommissionTransaction.CreateDirectSale(
                organization.Id,
                root.Id,
                orderId,
                orderItemId,
                plan.Id,
                "direct-sale",
                1_000m,
                0.10m,
                100m
            )
        );
        await AddAsync(
            BinaryVolumeEntry.Credit(
                organization.Id,
                root.Id,
                child.Id,
                orderItemId,
                PlacementSide.Left,
                250m,
                now
            )
        );

        var wallet = AgentWallet.Open(organization.Id, root.Id, "PHP");
        await AddAsync(wallet);
        await AddAsync(
            WalletEntry.Create(
                wallet.Id,
                WalletEntryType.AvailableCredit,
                100m,
                "Commission",
                Guid.NewGuid(),
                now
            )
        );
        await RunAsAdministratorAsync(organization.Id);

        var planResult = await SendAsync(new GetCommissionPlanQuery(organization.Id, plan.Id));
        var commissions = await SendAsync(
            new GetAdminCommissionLedgerQuery(
                organization.Id,
                1,
                20,
                root.Id,
                null,
                null,
                null,
                null,
                IncludeReversals: true
            )
        );
        var volume = await SendAsync(
            new GetBinaryVolumeLedgerQuery(
                organization.Id,
                root.Id,
                1,
                20,
                null,
                null,
                null,
                null
            )
        );
        var wallets = await SendAsync(
            new GetAdminWalletsQuery(organization.Id, 1, 20, null, null, false)
        );
        var entries = await SendAsync(
            new GetAdminWalletEntriesQuery(
                organization.Id,
                root.Id,
                1,
                20,
                null,
                null,
                null
            )
        );
        var downline = await SendAsync(
            new GetFilteredDownlineQuery(
                organization.Id,
                root.Id,
                1,
                20,
                null,
                null,
                null,
                false,
                null
            )
        );

        planResult.ShouldNotBeNull();
        planResult.Id.ShouldBe(plan.Id);
        commissions.TotalCount.ShouldBe(1);
        commissions.Totals.NetAmount.ShouldBe(100m);
        volume.TotalCount.ShouldBe(1);
        wallets.TotalCount.ShouldBe(1);
        wallets.Items.Single().Net.ShouldBe(100m);
        entries.TotalCount.ShouldBe(1);
        downline.TotalCount.ShouldBe(1);
        downline.Items.Single().AgentId.ShouldBe(child.Id);
    }

    private static Agent CreateActiveAgent(
        Guid organizationId,
        string code,
        DateTimeOffset joinedAt,
        Guid? sponsorId = null
    )
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var agent = Agent.Apply(
            organizationId,
            $"user-{suffix}",
            $"{code}-{suffix}",
            $"REF-{suffix}",
            joinedAt,
            sponsorId
        );
        agent.Activate(joinedAt);
        return agent;
    }
}
