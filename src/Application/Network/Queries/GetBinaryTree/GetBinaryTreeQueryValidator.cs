namespace modular_mlm.Application.Network.Queries.GetBinaryTree;

public sealed class GetBinaryTreeQueryValidator : AbstractValidator<GetBinaryTreeQuery>
{
    public GetBinaryTreeQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.Depth).InclusiveBetween(1, 10);
    }
}
