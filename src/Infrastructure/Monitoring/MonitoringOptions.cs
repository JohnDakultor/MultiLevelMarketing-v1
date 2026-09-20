namespace modular_mlm.Infrastructure.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int OutboxWarningCount { get; init; } = 100;
    public int OutboxUnhealthyCount { get; init; } = 1_000;
    public int DeadLetterWarningCount { get; init; } = 1;
}
