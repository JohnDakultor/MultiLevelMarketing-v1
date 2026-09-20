using modular_mlm.Domain.Compensation;
using modular_mlm.Infrastructure.BackgroundJobs;
using NUnit.Framework;
using Shouldly;

namespace modular_mlm.Application.UnitTests.BackgroundJobs;

public sealed class BinaryPairingScheduleProcessorTests
{
    [Test]
    public void ShouldCalculateThePreviousLocalDayInUtc()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        var now = new DateTimeOffset(2026, 8, 23, 10, 30, 0, TimeSpan.Zero);

        var period = BinaryPairingScheduleProcessor.CalculateMostRecentCompletedPeriod(
            now,
            timeZone,
            ProcessingFrequency.Daily
        );

        period.Start.ShouldBe(new DateTimeOffset(2026, 8, 21, 16, 0, 0, TimeSpan.Zero));
        period.End.ShouldBe(new DateTimeOffset(2026, 8, 22, 16, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void ShouldCalculateThePreviousCompletedMondayBasedWeek()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        var now = new DateTimeOffset(2026, 8, 23, 10, 30, 0, TimeSpan.Zero);

        var period = BinaryPairingScheduleProcessor.CalculateMostRecentCompletedPeriod(
            now,
            timeZone,
            ProcessingFrequency.Weekly
        );

        period.Start.ShouldBe(new DateTimeOffset(2026, 8, 9, 16, 0, 0, TimeSpan.Zero));
        period.End.ShouldBe(new DateTimeOffset(2026, 8, 16, 16, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void ShouldCalculateThePreviousCompletedCalendarMonth()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        var now = new DateTimeOffset(2026, 8, 23, 10, 30, 0, TimeSpan.Zero);

        var period = BinaryPairingScheduleProcessor.CalculateMostRecentCompletedPeriod(
            now,
            timeZone,
            ProcessingFrequency.Monthly
        );

        period.Start.ShouldBe(new DateTimeOffset(2026, 6, 30, 16, 0, 0, TimeSpan.Zero));
        period.End.ShouldBe(new DateTimeOffset(2026, 7, 31, 16, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void ShouldRespectDaylightSavingChangesAtPeriodBoundaries()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var now = new DateTimeOffset(2026, 3, 9, 12, 0, 0, TimeSpan.Zero);

        var period = BinaryPairingScheduleProcessor.CalculateMostRecentCompletedPeriod(
            now,
            timeZone,
            ProcessingFrequency.Daily
        );

        period.Start.ShouldBe(new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero));
        period.End.ShouldBe(new DateTimeOffset(2026, 3, 9, 4, 0, 0, TimeSpan.Zero));
        (period.End - period.Start).ShouldBe(TimeSpan.FromHours(23));
    }
}
