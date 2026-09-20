using FluentValidation;

namespace modular_mlm.Application.Wallets.Queries.GetWalletEntries;

public sealed class GetWalletEntriesQueryValidator : AbstractValidator<GetWalletEntriesQuery>
{
    public GetWalletEntriesQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.EntryType).IsInEnum().When(x => x.EntryType.HasValue);
        RuleFor(x => x.From).Must(BeUtcOffset).When(x => x.From.HasValue);
        RuleFor(x => x.To).Must(BeUtcOffset).When(x => x.To.HasValue);
        RuleFor(x => new { x.From, x.To })
            .Must(dates =>
                !dates.From.HasValue || !dates.To.HasValue || dates.From.Value < dates.To.Value
            )
            .WithMessage("From date must be less than To date.");
    }

    private bool BeUtcOffset(DateTimeOffset? date)
    {
        return date?.Offset == TimeSpan.Zero;
    }
}
