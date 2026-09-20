using modular_mlm.Application.Reporting;

namespace modular_mlm.Application.Reporting.Queries.GetAdminReport;

public sealed class GetAdminReportQueryValidator : AbstractValidator<GetAdminReportQuery>
{
    public GetAdminReportQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        ReportingPeriod.Configure(this, x => x.From, x => x.To);
    }
}
