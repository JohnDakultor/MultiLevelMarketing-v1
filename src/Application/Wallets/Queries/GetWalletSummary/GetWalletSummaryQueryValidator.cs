namespace modular_mlm.Application.Wallets.Queries.GetWalletSummary;

public sealed class GetWalletSummaryQueryValidator : AbstractValidator<GetWalletSummaryQuery>
{
    public GetWalletSummaryQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
