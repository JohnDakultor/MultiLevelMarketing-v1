namespace modular_mlm.Application.Inventory.Queries.GetInventory;

public sealed class GetInventoryQueryValidator : AbstractValidator<GetInventoryQuery>
{
    public GetInventoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.Status!.Value).IsInEnum().When(query => query.Status.HasValue);
    }
}
