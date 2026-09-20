namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class InventoryReservationCleanupOptions
{
    public const string SectionName = "BackgroundJobs:InventoryReservationCleanup";

    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 60;
    public int OrganizationBatchSize { get; init; } = 50;
    public int ReservationBatchSize { get; init; } = 100;
    public int ReservationLifetimeMinutes { get; init; } = 30;
}
