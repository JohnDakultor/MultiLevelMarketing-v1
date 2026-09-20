namespace modular_mlm.Application.Network.Queries.GetAgentLegSummary.Models;

public sealed record AgentLegSummaryDto(
    Guid AgentId,
    int LeftAgentCount,
    int RightAgentCount,
    decimal LeftAvailableVolume,
    decimal RightAvailableVolume,
    decimal LeftLifetimeVolume,
    decimal RightLifetimeVolume
);
