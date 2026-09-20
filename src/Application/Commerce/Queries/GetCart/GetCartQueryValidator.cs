namespace modular_mlm.Application.Commerce.Queries.GetCart;

public sealed class GetCartQueryValidator : AbstractValidator<GetCartQuery>
{
    public GetCartQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
    }
}
