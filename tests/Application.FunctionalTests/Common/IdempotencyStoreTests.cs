using Microsoft.Extensions.DependencyInjection;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Application.FunctionalTests.Common;

using static Infrastructure.TestApp;

public sealed class IdempotencyStoreTests : TestBase
{
    [Test]
    public async Task CleanupRunsWithTheConfiguredRetryingExecutionStrategy()
    {
        var result = await ExecuteInScopeAsync(services =>
            services
                .GetRequiredService<IdempotencyCleanupJob>()
                .ExecuteAsync(CancellationToken.None)
        );

        result.LockAcquired.ShouldBeTrue();
        result.DeletedCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task StartCompleteAndReplayReturnsProtectedOutcome()
    {
        await ExecuteInScopeAsync(async services =>
        {
            var store = services.GetRequiredService<IIdempotencyStore>();
            var expiry = DateTimeOffset.UtcNow.AddMinutes(5);
            var first = await store.TryBeginAsync(
                null,
                "checkout",
                "key-1",
                "request-a",
                expiry,
                CancellationToken.None
            );
            first.Kind.ShouldBe(IdempotencyDecisionKind.Started);
            var second = await store.TryBeginAsync(
                null,
                "checkout",
                "key-1",
                "request-a",
                expiry,
                CancellationToken.None
            );
            second.Kind.ShouldBe(IdempotencyDecisionKind.InProgress);
            await store.CompleteAsync(
                null,
                "checkout",
                "key-1",
                "request-a",
                201,
                "{\"id\":\"result\"}",
                DateTimeOffset.UtcNow,
                CancellationToken.None
            );
            var completed = await store.GetAsync(
                null,
                "checkout",
                "key-1",
                "request-a",
                CancellationToken.None
            );
            completed!.Kind.ShouldBe(IdempotencyDecisionKind.Completed);
            completed.StatusCode.ShouldBe(201);
            completed.Outcome.ShouldBe("{\"id\":\"result\"}");
            return true;
        });
    }

    [Test]
    public async Task ReusingKeyForDifferentPayloadConflicts()
    {
        await ExecuteInScopeAsync(async services =>
        {
            var store = services.GetRequiredService<IIdempotencyStore>();
            var expiry = DateTimeOffset.UtcNow.AddMinutes(5);
            await store.TryBeginAsync(
                null,
                "payment",
                "same-key",
                "request-a",
                expiry,
                CancellationToken.None
            );
            var decision = await store.TryBeginAsync(
                null,
                "payment",
                "same-key",
                "request-b",
                expiry,
                CancellationToken.None
            );
            decision.Kind.ShouldBe(IdempotencyDecisionKind.Conflict);
            return true;
        });
    }
}
