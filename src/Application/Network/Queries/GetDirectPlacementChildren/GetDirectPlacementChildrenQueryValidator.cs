namespace modular_mlm.Application.Network.Queries.GetDirectPlacementChildren;

public sealed class GetDirectPlacementChildrenQueryValidator
    : AbstractValidator<GetDirectPlacementChildrenQuery>
{
    public GetDirectPlacementChildrenQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
