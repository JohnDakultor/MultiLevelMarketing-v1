namespace modular_mlm.Application.Compensation.Commands.PublishCommissionPlan;

public sealed record PublishCommissionPlanCommand(Guid OrganizationId, Guid CommissionPlanId)
    : IRequest,
        IOrganizationAdminRequest;
