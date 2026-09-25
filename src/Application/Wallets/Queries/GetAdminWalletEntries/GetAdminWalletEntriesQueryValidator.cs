namespace modular_mlm.Application.Wallets.Queries.GetAdminWalletEntries;

public sealed class GetAdminWalletEntriesQueryValidator
    : AbstractValidator<GetAdminWalletEntriesQuery>
{
    public GetAdminWalletEntriesQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.EntryType)
            .IsInEnum()
            .When(query => query.EntryType.HasValue);
        RuleFor(query => query.From)
            .LessThanOrEqualTo(query => query.To)
            .When(query => query.From.HasValue && query.To.HasValue);
    }
}
