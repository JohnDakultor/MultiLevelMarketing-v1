namespace modular_mlm.Application.Commerce.Queries.GetAdminOrders;

public sealed class GetAdminOrdersQueryValidator : AbstractValidator<GetAdminOrdersQuery>
{
    public GetAdminOrdersQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.Status!.Value).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query.PaymentStatus!.Value)
            .IsInEnum()
            .When(query => query.PaymentStatus.HasValue);
        RuleFor(query => query.CreatedTo)
            .GreaterThanOrEqualTo(query => query.CreatedFrom)
            .When(query => query.CreatedFrom.HasValue && query.CreatedTo.HasValue);
    }
}
