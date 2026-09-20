namespace modular_mlm.Application.Inventory.Queries.GetInventoryHistory;

public sealed class GetInventoryHistoryQueryValidator : AbstractValidator<GetInventoryHistoryQuery>
{
    public GetInventoryHistoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.ProductVariantId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
