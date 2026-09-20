using modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Compensation;

using static Infrastructure.TestApp;

public sealed class ProcessPaidOrderCommissionsTests : TestBase
{
    [Test]
    public async Task ShouldCreateDirectCommissionWalletCreditAndAncestorVolume()
    {
        var data = await CreatePaidOrderDataAsync();

        var result = await SendAsync(
            new ProcessPaidOrderCommissionsCommand(data.OrganizationId, data.OrderId)
        );

        result.DirectCommissionsCreated.ShouldBe(1);
        result.VolumeCreditsCreated.ShouldBe(1);
        result.GrossCommissionAmount.ShouldBe(100m);

        var commission = await SingleAsync<CommissionTransaction>(transaction =>
            transaction.SourceOrderId == data.OrderId
        );
        commission.BeneficiaryAgentId.ShouldBe(data.SellingAgentId);
        commission.Type.ShouldBe(CommissionType.DirectSale);
        commission.Status.ShouldBe(CommissionStatus.Pending);
        commission.BaseAmount.ShouldBe(1_000m);
        commission.Rate.ShouldBe(0.10m);
        commission.Amount.ShouldBe(100m);

        var wallet = await SingleAsync<AgentWallet>(candidate =>
            candidate.AgentId == data.SellingAgentId
        );
        wallet.Currency.ShouldBe("PHP");
        var walletEntry = await SingleAsync<WalletEntry>(entry => entry.WalletId == wallet.Id);
        walletEntry.Type.ShouldBe(WalletEntryType.PendingCredit);
        walletEntry.Amount.ShouldBe(100m);
        walletEntry.SourceId.ShouldBe(commission.Id);

        var volume = await SingleAsync<BinaryVolumeEntry>(entry =>
            entry.SourceAgentId == data.SellingAgentId
        );
        volume.OwnerAgentId.ShouldBe(data.UplineAgentId);
        volume.Side.ShouldBe(PlacementSide.Left);
        volume.Volume.ShouldBe(100m);
        var balance = await SingleAsync<BinaryVolumeBalance>(candidate =>
            candidate.AgentId == data.UplineAgentId
        );
        balance.LeftAvailable.ShouldBe(100m);
        balance.LeftLifetime.ShouldBe(100m);
    }

    [Test]
    public async Task ShouldBeIdempotentWhenThePaidOrderIsProcessedAgain()
    {
        var data = await CreatePaidOrderDataAsync();
        var command = new ProcessPaidOrderCommissionsCommand(data.OrganizationId, data.OrderId);

        await SendAsync(command);
        var repeated = await SendAsync(command);

        repeated.DirectCommissionsCreated.ShouldBe(0);
        repeated.VolumeCreditsCreated.ShouldBe(0);
        repeated.GrossCommissionAmount.ShouldBe(0m);
        (await CountAsync<CommissionTransaction>()).ShouldBe(1);
        (await CountAsync<WalletEntry>()).ShouldBe(1);
        (await CountAsync<BinaryVolumeEntry>()).ShouldBe(1);
    }

    private static async Task<PaidOrderData> CreatePaidOrderDataAsync()
    {
        var paidAt = DateTimeOffset.UtcNow;
        var organization = Organization.Create(
            "Philippine Compensation Test",
            $"ph-compensation-{Guid.NewGuid():N}",
            "PHP",
            "Asia/Manila",
            "en-PH"
        );
        var upline = CreateActiveAgent(organization.Id, "UPLINE");
        var sellingAgent = CreateActiveAgent(organization.Id, "SELLER");
        var closure = PlacementClosure.Create(
            organization.Id,
            upline.Id,
            sellingAgent.Id,
            1,
            PlacementSide.Left
        );
        var plan = CommissionPlan.Draft(
            organization.Id,
            "Philippine Retail Sales Plan",
            1,
            paidAt.AddDays(-1)
        );
        plan.ConfigureDirectSales(0.10m);
        plan.Publish();
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            sellingAgent.Id,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Retail Product",
            "PH-SKU",
            1_000m,
            1,
            1_000m,
            null,
            100m,
            Guid.NewGuid()
        );
        order.MarkPaid(paidAt);

        await AddAsync(organization);
        await AddAsync(upline);
        await AddAsync(sellingAgent);
        await AddAsync(closure);
        await AddAsync(plan);
        await AddAsync(order);

        return new PaidOrderData(organization.Id, order.Id, sellingAgent.Id, upline.Id);
    }

    private static Agent CreateActiveAgent(Guid organizationId, string code)
    {
        var agent = Agent.Apply(
            organizationId,
            Guid.NewGuid().ToString(),
            code,
            $"REF-{code}",
            DateTimeOffset.UtcNow
        );
        agent.Activate(DateTimeOffset.UtcNow);
        return agent;
    }

    private sealed record PaidOrderData(
        Guid OrganizationId,
        Guid OrderId,
        Guid SellingAgentId,
        Guid UplineAgentId
    );
}
