using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetAdminCommissionLedger;

public sealed class GetAdminCommissionLedgerQueryValidator
    : AbstractValidator<GetAdminCommissionLedgerQuery>
{
    public GetAdminCommissionLedgerQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.AgentId)
            .NotEqual(Guid.Empty)
            .When(query => query.AgentId.HasValue);
        RuleFor(query => query.From)
            .LessThanOrEqualTo(query => query.To)
            .When(query => query.From.HasValue && query.To.HasValue);
        RuleFor(query => query.CommissionType)
            .Must(value => TryParseEnum<CommissionType>(value))
            .When(query => !string.IsNullOrWhiteSpace(query.CommissionType))
            .WithMessage("CommissionType is invalid.");
        RuleFor(query => query.Status)
            .Must(value => TryParseEnum<CommissionStatus>(value))
            .When(query => !string.IsNullOrWhiteSpace(query.Status))
            .WithMessage("Status is invalid.");
    }

    private static bool TryParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value?.Trim(), ignoreCase: true, out var parsed)
        && Enum.IsDefined(parsed);
}
