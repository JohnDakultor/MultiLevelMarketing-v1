namespace modular_mlm.Application.Compensation.Commands.ProcessPaidOrderCommissions;

public sealed record ProcessPaidOrderCommissionsResult(
    Guid OrderId,
    Guid CommissionPlanId,
    int DirectCommissionsCreated,
    int VolumeCreditsCreated,
    decimal GrossCommissionAmount
);

public sealed record ProcessPaidOrderCommissionsCommand(Guid OrganizationId, Guid OrderId)
    : IRequest<ProcessPaidOrderCommissionsResult>;
