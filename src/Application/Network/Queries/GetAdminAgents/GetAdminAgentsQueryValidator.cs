namespace modular_mlm.Application.Network.Queries.GetAdminAgents;

public sealed class GetAdminAgentsQueryValidator : AbstractValidator<GetAdminAgentsQuery>
{
    public GetAdminAgentsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status!.Value).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query.Placement).IsInEnum();
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
