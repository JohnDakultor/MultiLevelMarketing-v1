namespace modular_mlm.Application.Network.Queries.GetDownline;

public sealed class GetDownlineQueryValidator : AbstractValidator<GetDownlineQuery>
{
    public GetDownlineQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.MaxDepth).InclusiveBetween(1, 100);
    }
}
