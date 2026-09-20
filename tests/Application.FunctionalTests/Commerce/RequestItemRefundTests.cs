using modular_mlm.Application.Commerce.Commands.RequestItemRefund;
using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.FunctionalTests.Infrastructure;
using modular_mlm.Domain.AuditLogs;
using modular_mlm.Domain.Commerce;
using modular_mlm.Domain.Organizations;
using modular_mlm.Domain.Payments;

namespace modular_mlm.Application.FunctionalTests.Commerce;

using static Infrastructure.TestApp;

public sealed class RequestItemRefundTests : TestBase
{
    [Test]
    public async Task ShouldCalculatePartialAllocationAndReuseAPendingRequest()
    {
        var (organization, order, item, _) = await CreatePaidOrderAsync();
        await RunAsAdministratorAsync(organization.Id);
        var gateway = GetRequiredService<TestPaymentGateway>();
        var command = new RequestItemRefundCommand(
            organization.Id,
            order.Id,
            item.Id,
            1m,
            "requested_by_customer"
        );

        var firstId = await SendAsync(command);
        var secondId = await SendAsync(command);

        secondId.ShouldBe(firstId);
        gateway.RefundCallCount.ShouldBe(1);
        gateway.LastRefundRequest.ShouldNotBeNull();
        gateway.LastRefundRequest.AmountInMinorUnits.ShouldBe(10_000);
        gateway.LastRefundRequest.IdempotencyKey.ShouldStartWith("paymongo-refund:");
        var refund = await FindAsync<OrderItemRefund>(firstId);
        refund.ShouldNotBeNull();
        refund.Quantity.ShouldBe(1m);
        refund.RefundAmount.ShouldBe(100m);
        refund.CommissionableAmountToReverse.ShouldBe(75m);
        refund.BusinessVolumeToReverse.ShouldBe(20m);
        refund.Status.ShouldBe(OrderItemRefundStatus.Pending);
        var audit = await SingleAsync<AuditLog>(entry =>
            entry.EntityId == firstId
            && entry.Action == AuditCoverageMap.OrderItemRefundRequested.Action
        );
        audit.Reason.ShouldBe("requested_by_customer");
        audit.OrganizationId.ShouldBe(organization.Id);
        (
            await CountAsync<AuditLog>(entry =>
                entry.EntityId == firstId
                && entry.Action == AuditCoverageMap.OrderItemRefundRequested.Action
            )
        ).ShouldBe(1);
    }

    [Test]
    public async Task ShouldRejectCrossOrganizationRefundRequest()
    {
        var (organization, order, item, _) = await CreatePaidOrderAsync();
        var otherOrganization = Organization.Create(
            "Other tenant",
            $"other-{Guid.NewGuid():N}",
            "PHP"
        );
        await AddAsync(otherOrganization);
        await RunAsAdministratorAsync(otherOrganization.Id);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(
                new RequestItemRefundCommand(
                    organization.Id,
                    order.Id,
                    item.Id,
                    1m,
                    "requested_by_customer"
                )
            )
        );
    }

    internal static async Task<(
        Organization Organization,
        Order Order,
        OrderItem Item,
        Payment Payment
    )> CreatePaidOrderAsync()
    {
        var organization = Organization.Create(
            "Item Refund Test",
            $"item-refund-{Guid.NewGuid():N}",
            "PHP"
        );
        var order = Order.Create(
            organization.Id,
            $"ORD-{Guid.NewGuid():N}",
            Guid.NewGuid(),
            null,
            "PHP",
            "{}",
            "{}"
        );
        order.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Four-pack",
            $"SKU-{Guid.NewGuid():N}",
            100m,
            4,
            300m,
            null,
            80m,
            null
        );
        var item = order.Items.Single();
        order.MarkPaid(DateTimeOffset.UtcNow);
        var payment = Payment.Initiate(
            organization.Id,
            order.Id,
            "PayMongo",
            $"payment:{order.Id:N}",
            order.GrandTotal,
            order.Currency
        );
        payment.MarkPaid($"pay_{Guid.NewGuid():N}", DateTimeOffset.UtcNow);

        await AddAsync(organization);
        await AddAsync(order);
        await AddAsync(payment);
        return (organization, order, item, payment);
    }
}
