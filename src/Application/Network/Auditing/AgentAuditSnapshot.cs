using System.Text.Json;
using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Auditing;

internal sealed record AgentAuditSnapshot(
    AgentStatus Status,
    DateTimeOffset? ActivatedAt,
    Guid? SponsorAgentId,
    Guid? PlacementParentAgentId,
    PlacementSide? PlacementSide,
    PlacementSide? PreferredLeg,
    string ReferralCode,
    string QualificationState
)
{
    public static string Serialize(Agent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return JsonSerializer.Serialize(
            new AgentAuditSnapshot(
                agent.Status,
                agent.ActivatedAt,
                agent.SponsorAgentId,
                agent.PlacementParentAgentId,
                agent.PlacementSide,
                agent.PreferredLeg,
                agent.ReferralCode,
                agent.QualificationState
            )
        );
    }
}
