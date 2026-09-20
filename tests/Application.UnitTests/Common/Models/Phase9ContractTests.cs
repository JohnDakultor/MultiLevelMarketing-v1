using modular_mlm.Application.Common.Models;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.Common.Models;

public sealed class Phase9ContractTests
{
    [Test]
    public void DurableJobPayloadNormalizesRegisteredJobName()
    {
        var payload = new DurableJobPayload(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " PROCESS-PAYOUT ",
            "{}",
            "payout:1",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            null
        );

        payload.JobName.ShouldBe(DurableJobRegistry.ProcessCommissionPayout);
    }

    [Test]
    public void DurableJobPayloadRejectsUnknownJobName()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new DurableJobPayload(
                Guid.NewGuid(),
                null,
                "arbitrary-clr-type",
                "{}",
                "key",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                null
            )
        );
    }

    [Test]
    public void CompletedIdempotencyDecisionCarriesReplayableOutcome()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var decision = IdempotencyDecision.Completed(201, "{\"id\":\"result\"}", expiresAt);

        decision.Kind.ShouldBe(IdempotencyDecisionKind.Completed);
        decision.StatusCode.ShouldBe(201);
        decision.Outcome.ShouldBe("{\"id\":\"result\"}");
        decision.ExpiresAt.ShouldBe(expiresAt);
    }

    [Test]
    public void NotificationRequestCopiesVariables()
    {
        var variables = new Dictionary<string, string> { ["name"] = "Agent" };
        var request = new NotificationDeliveryRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            "agent@example.com",
            NotificationChannel.Email,
            "payout-approved",
            "en-PH",
            variables
        );

        variables["name"] = "Changed";

        request.Variables["name"].ShouldBe("Agent");
    }

    [Test]
    public void DeliveryResultDistinguishesTransientAndPermanentFailures()
    {
        var transient = NotificationDeliveryResult.TransientFailure(
            "provider-unavailable",
            TimeSpan.FromMinutes(1)
        );
        var permanent = NotificationDeliveryResult.PermanentFailure("invalid-recipient");

        transient.Succeeded.ShouldBeFalse();
        transient.IsTransient.ShouldBeTrue();
        transient.RetryAfter.ShouldBe(TimeSpan.FromMinutes(1));
        permanent.IsTransient.ShouldBeFalse();
        permanent.RetryAfter.ShouldBeNull();
    }
}
