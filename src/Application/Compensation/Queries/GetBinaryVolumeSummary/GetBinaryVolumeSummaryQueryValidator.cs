namespace modular_mlm.Application.Compensation.Queries.GetBinaryVolumeSummary;

public sealed class GetBinaryVolumeSummaryQueryValidator
    : AbstractValidator<GetBinaryVolumeSummaryQuery>
{
    public GetBinaryVolumeSummaryQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.AgentId).NotEmpty();
    }
}
