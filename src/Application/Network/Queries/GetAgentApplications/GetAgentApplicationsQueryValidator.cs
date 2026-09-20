using modular_mlm.Domain.Network;

namespace modular_mlm.Application.Network.Queries.GetAgentApplications;

public sealed class GetAgentApplicationsQueryValidator
    : AbstractValidator<GetAgentApplicationsQuery>
{
    public GetAgentApplicationsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status)
            .Must(status =>
                status
                    is null
                        or AgentStatus.Applied
                        or AgentStatus.PendingApproval
                        or AgentStatus.Closed
            )
            .WithMessage("Status is not an Agent application state.");
    }
}
