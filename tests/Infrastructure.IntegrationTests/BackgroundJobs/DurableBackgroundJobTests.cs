using modular_mlm.Application.Common.Models;
using modular_mlm.Infrastructure.BackgroundJobs;
using Shouldly;

namespace modular_mlm.Infrastructure.IntegrationTests.BackgroundJobs;

public sealed class DurableBackgroundJobTests
{
    [Test]
    public void CreateRejectsUnknownJobName()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            DurableBackgroundJob.Create(
                Guid.NewGuid(),
                "unknown",
                "{}",
                "key",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow
            )
        );
    }

    [Test]
    public void FailuresBackoffAndEventuallyDeadLetter()
    {
        var now = DateTimeOffset.UtcNow;
        var job = DurableBackgroundJob.Create(
            Guid.NewGuid(),
            DurableJobRegistry.ProcessCommissionPayout,
            "{}",
            "key",
            Guid.NewGuid(),
            now
        );

        job.MarkFailed("temporary", now, 2, TimeSpan.FromSeconds(30)).ShouldBeFalse();
        job.NextAttemptAt.ShouldBe(now.AddSeconds(30));
        job.MarkFailed("temporary", now.AddSeconds(30), 2, TimeSpan.FromSeconds(30)).ShouldBeTrue();
        job.DeadLetteredAt.ShouldNotBeNull();
    }
}
