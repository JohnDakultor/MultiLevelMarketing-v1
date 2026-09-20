namespace modular_mlm.Application.Commerce.Queries.GetRefundHistory;

public sealed class GetRefundHistoryQueryValidator : AbstractValidator<GetRefundHistoryQuery>
{
    public GetRefundHistoryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.OrderId).NotEmpty().When(query => query.OrderId.HasValue);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
