namespace modular_mlm.Application.Compensation.Commands.RetireCommissionPlan;

public sealed record RetireCommissionPlanCommand(
    Guid OrganizationId,
    Guid CommissionPlanId,
    DateTimeOffset EffectiveTo,
    string Reason
) : IRequest, IOrganizationAdminRequest;
