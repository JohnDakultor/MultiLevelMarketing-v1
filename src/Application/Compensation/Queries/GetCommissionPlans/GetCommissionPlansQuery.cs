using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlans;

public sealed record GetCommissionPlansQuery(Guid OrganizationId)
    : IRequest<IReadOnlyList<CommissionPlanDto>>,
        IOrganizationAdminRequest;
