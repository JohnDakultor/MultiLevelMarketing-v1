namespace modular_mlm.Application.Reporting.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryValidator : AbstractValidator<GetAdminDashboardQuery>
{
    public GetAdminDashboardQueryValidator() => RuleFor(x => x.OrganizationId).NotEmpty();
}
