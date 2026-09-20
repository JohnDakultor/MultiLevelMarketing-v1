using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using modular_mlm.Domain.Wallets;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Application.FunctionalTests.Wallets;

using static Infrastructure.TestApp;

public sealed class CommissionReleaseJobTests : TestBase
{
    [Test]
    public async Task JobRotatesAcrossBoundedOrganizationBatches()
    {
        var first = await ReleasePendingCommissionsTests.CreatePendingCommissionAsync(0);
        var second = await ReleasePendingCommissionsTests.CreatePendingCommissionAsync(0);

        var firstRun = await ExecuteJobAsync();
        var secondRun = await ExecuteJobAsync();

        firstRun.OrganizationsProcessed.ShouldBe(1);
        secondRun.OrganizationsProcessed.ShouldBe(1);
        (firstRun.CommissionsReleased + secondRun.CommissionsReleased).ShouldBe(2);
        (await CountAsync<WalletEntry>(entry => entry.ReleasedFromEntryId != null)).ShouldBe(2);
        (
            await CountAsync<WalletEntry>(entry =>
                entry.ReleasedFromEntryId == first.PendingEntryId
                || entry.ReleasedFromEntryId == second.PendingEntryId
            )
        ).ShouldBe(2);
    }

    [Test]
    public async Task WorkerProcessesPostgreSqlRecordsUsingAFreshScope()
    {
        var data = await ReleasePendingCommissionsTests.CreatePendingCommissionAsync(0);
        var worker = new CommissionReleaseWorker(
            FunctionalTestSetup.ScopeFactory,
            Options.Create(
                new CommissionReleaseOptions
                {
                    Enabled = true,
                    PollIntervalSeconds = 5,
                    OrganizationBatchSize = 1,
                    CommissionBatchSize = 1,
                    FailureBackoffSeconds = 1,
                }
            ),
            NullLogger<CommissionReleaseWorker>.Instance
        );

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await WaitUntilAsync(
                async () =>
                    await CountAsync<WalletEntry>(entry =>
                        entry.ReleasedFromEntryId == data.PendingEntryId
                    ) == 1,
                TimeSpan.FromSeconds(5)
            );
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [Test]
    public async Task DisabledWorkerDoesNotProcessWalletEntries()
    {
        var data = await ReleasePendingCommissionsTests.CreatePendingCommissionAsync(0);
        var worker = new CommissionReleaseWorker(
            FunctionalTestSetup.ScopeFactory,
            Options.Create(new CommissionReleaseOptions { Enabled = false }),
            NullLogger<CommissionReleaseWorker>.Instance
        );

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);
        worker.Dispose();

        (
            await CountAsync<WalletEntry>(entry => entry.ReleasedFromEntryId == data.PendingEntryId)
        ).ShouldBe(0);
    }

    private static Task<CommissionReleaseJobResult> ExecuteJobAsync() =>
        ExecuteInScopeAsync(provider =>
            provider.GetRequiredService<CommissionReleaseJob>().ExecuteAsync(CancellationToken.None)
        );

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
                return;
            await Task.Delay(50);
        }

        throw new TimeoutException("The commission release worker did not finish in time.");
    }
}
