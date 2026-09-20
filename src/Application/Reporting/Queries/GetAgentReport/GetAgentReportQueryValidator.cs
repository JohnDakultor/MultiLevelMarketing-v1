using modular_mlm.Application.Reporting;

namespace modular_mlm.Application.Reporting.Queries.GetAgentReport;

public sealed class GetAgentReportQueryValidator : AbstractValidator<GetAgentReportQuery>
{
    public GetAgentReportQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        ReportingPeriod.Configure(this, x => x.From, x => x.To);
    }
}
