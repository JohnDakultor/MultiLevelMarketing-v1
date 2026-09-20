using modular_mlm.Domain.Payouts;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Domain.UnitTests.Payouts;

public sealed class PayoutRequestTests
{
    [Test]
    public void ShouldFollowTheProviderBackedPayoutLifecycle()
    {
        var payout = PayoutRequest.Request(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            500m,
            "PHP",
            DateTimeOffset.UtcNow
        );

        payout.StartReview();
        payout.Approve(DateTimeOffset.UtcNow);
        payout.StartProcessing();
        payout.AttachProviderTransfer("batch_test", "transfer_test");
        payout.MarkPaid("provider_reference", DateTimeOffset.UtcNow);

        payout.Status.ShouldBe(PayoutStatus.Paid);
        payout.ProviderBatchId.ShouldBe("batch_test");
        payout.ProviderTransferId.ShouldBe("transfer_test");
    }
}
