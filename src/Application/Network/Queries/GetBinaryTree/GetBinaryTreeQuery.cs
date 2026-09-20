using modular_mlm.Application.Network.Queries.GetBinaryTree.Models;

namespace modular_mlm.Application.Network.Queries.GetBinaryTree;

public sealed record GetBinaryTreeQuery(Guid OrganizationId, Guid AgentId, int Depth = 3)
    : IRequest<IReadOnlyList<BinaryTreeNodeDto>>,
        IAgentScopedRequest;
