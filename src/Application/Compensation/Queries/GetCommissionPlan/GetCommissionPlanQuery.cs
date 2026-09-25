using modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlan;

public sealed record GetCommissionPlanQuery(Guid OrganizationId, Guid CommissionPlanId)
    : IRequest<CommissionPlanDto?>, IOrganizationAdminRequest;
