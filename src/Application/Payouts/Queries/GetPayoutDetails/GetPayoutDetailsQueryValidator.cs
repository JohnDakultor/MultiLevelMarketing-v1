namespace modular_mlm.Application.Payouts.Queries.GetPayoutDetails;

public sealed class GetPayoutDetailsQueryValidator : AbstractValidator<GetPayoutDetailsQuery>
{
    public GetPayoutDetailsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.PayoutRequestId).NotEmpty();
    }
}
