namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class CommissionReleaseOptions
{
    public const string SectionName = "BackgroundJobs:CommissionRelease";

    public bool Enabled { get; init; }
    public int PollIntervalSeconds { get; init; } = 60;
    public int OrganizationBatchSize { get; init; } = 50;
    public int CommissionBatchSize { get; init; } = 100;
    public int FailureBackoffSeconds { get; init; } = 15;
}
