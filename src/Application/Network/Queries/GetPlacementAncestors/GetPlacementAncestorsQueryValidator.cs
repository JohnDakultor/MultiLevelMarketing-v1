namespace modular_mlm.Application.Network.Queries.GetPlacementAncestors;

public sealed class GetPlacementAncestorsQueryValidator
    : AbstractValidator<GetPlacementAncestorsQuery>
{
    public GetPlacementAncestorsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
