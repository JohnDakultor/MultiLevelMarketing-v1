using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetBinaryTree.Models;

public sealed record BinaryTreeNodeDto(
    Guid AgentId,
    string AgentCode,
    string ReferralCode,
    AgentStatus Status,
    Guid? ParentAgentId,
    PlacementSide? Side,
    int Depth
);
