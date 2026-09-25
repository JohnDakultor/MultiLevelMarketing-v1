namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeLedger;

public sealed class GetBinaryVolumeLedgerQueryValidator
    : AbstractValidator<GetBinaryVolumeLedgerQuery>
{
    public GetBinaryVolumeLedgerQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.From)
            .LessThanOrEqualTo(query => query.To)
            .When(query => query.From.HasValue && query.To.HasValue);
        RuleFor(query => query.Side)
            .IsInEnum()
            .When(query => query.Side.HasValue);
        RuleFor(query => query.EntryType)
            .IsInEnum()
            .When(query => query.EntryType.HasValue);
    }
}
