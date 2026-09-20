namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary.Models;

public sealed record BinaryVolumeSummaryDto(
    Guid AgentId,
    decimal LeftAvailable,
    decimal RightAvailable,
    decimal LeftLifetime,
    decimal RightLifetime
);
