using modular_mlm.Application.Commerce.Commands.ProcessItemRefundReversal;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Network;
using modular_mlm.Domain.Payments;
using modular_mlm.Domain.Wallets;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class ProcessItemRefundReversalTests : TestBase
{
    [Test]
    public async Task ShouldCreateExactLinkedLedgerReversalsOnlyOnce()
    {
        var (organization, order, item, payment) =
            await RequestItemRefundTests.CreatePaidOrderAsync();
        await RunAsAdministratorAsync(organization.Id);
        var now = DateTimeOffset.UtcNow;
        var paymentRefund = PaymentRefund.Request(
            organization.Id,
            payment.Id,
            order.Id,
            100m,
            "PHP",
            "requested_by_customer",
            now
        );
        paymentRefund.AttachProviderResult("refund_succeeded", "succeeded", now);
        var itemRefund = OrderItemRefund.Create(
            organization.Id,
            order.Id,
            item.Id,
            paymentRefund.Id,
            1m,
            100m,
            75m,
            20m,
            4m
        );
        var agentId = Guid.NewGuid();
        var commission = CommissionTransaction.CreateDirectSale(
            organization.Id,
            agentId,
            order.Id,
            item.Id,
            Guid.NewGuid(),
            "direct-sale",
            300m,
            0.10m,
            30m
        );
        var wallet = AgentWallet.Open(organization.Id, agentId, "PHP");
        var walletCredit = WalletEntry.Create(
            wallet.Id,
            WalletEntryType.AvailableCredit,
            commission.Amount,
            "Commission",
            commission.Id,
            now
        );
        var volumeCredit = BinaryVolumeEntry.Credit(
            organization.Id,
            agentId,
            agentId,
            item.Id,
            PlacementSide.Left,
            80m,
            now
        );
        var volumeBalance = BinaryVolumeBalance.Open(organization.Id, agentId);
        volumeBalance.Credit(PlacementSide.Left, 80m);
        await AddAsync(paymentRefund);
        await AddAsync(itemRefund);
        await AddAsync(commission);
        await AddAsync(wallet);
        await AddAsync(walletCredit);
        await AddAsync(volumeCredit);
        await AddAsync(volumeBalance);

        var command = new ProcessItemRefundReversalCommand(organization.Id, itemRefund.Id);
        (await SendAsync(command)).ShouldBeTrue();
        (await SendAsync(command)).ShouldBeFalse();

        var updatedRefund = await FindAsync<OrderItemRefund>(itemRefund.Id);
        updatedRefund!.Status.ShouldBe(OrderItemRefundStatus.Reversed);
        var commissionReversal = await SingleAsync<CommissionTransaction>(entry =>
            entry.SourceOrderItemRefundId == itemRefund.Id
        );
        commissionReversal.Amount.ShouldBe(-7.50m);
        commissionReversal.ReversalOfCommissionId.ShouldBe(commission.Id);
        var volumeReversal = await SingleAsync<BinaryVolumeEntry>(entry =>
            entry.SourceOrderItemRefundId == itemRefund.Id
        );
        volumeReversal.Volume.ShouldBe(-20m);
        var walletReversal = await SingleAsync<WalletEntry>(entry =>
            entry.SourceType == "OrderItemRefund" && entry.SourceId == itemRefund.Id
        );
        walletReversal.Amount.ShouldBe(-7.50m);
        (await FindAsync<BinaryVolumeBalance>(volumeBalance.Id))!.LeftAvailable.ShouldBe(60m);
        (await FindAsync<Payment>(payment.Id))!.RefundedAmount.ShouldBe(100m);
        (await CountAsync<AuditLog>(audit => audit.EntityId == itemRefund.Id)).ShouldBe(1);
        (
            await CountAsync<CommissionTransaction>(entry =>
                entry.SourceOrderItemRefundId == itemRefund.Id
            )
        ).ShouldBe(1);
        (
            await CountAsync<BinaryVolumeEntry>(entry =>
                entry.SourceOrderItemRefundId == itemRefund.Id
            )
        ).ShouldBe(1);
    }
}
