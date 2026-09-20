namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class AdministratorInvitationExpiryOptions
{
    public const string SectionName = "BackgroundJobs:AdministratorInvitationExpiry";
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 300;
    public int OrganizationBatchSize { get; init; } = 50;
    public int InvitationBatchSize { get; init; } = 100;
}
