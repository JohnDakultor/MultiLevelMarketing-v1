namespace modular_mlm.Application.Network.Queries.GetDirectRecruits;

public sealed class GetDirectRecruitsQueryValidator : AbstractValidator<GetDirectRecruitsQuery>
{
    public GetDirectRecruitsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
