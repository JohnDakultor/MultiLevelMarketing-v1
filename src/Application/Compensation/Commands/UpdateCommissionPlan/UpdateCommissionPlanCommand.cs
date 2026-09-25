using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Commands.UpdateCommissionPlan;

public sealed record UpdateCommissionPlanCommand(
    Guid OrganizationId,
    Guid CommissionPlanId,
    bool DirectSalesEnabled,
    decimal DirectSalesRate,
    bool BinaryPairingEnabled,
    PairingCalculationType PairingCalculationType,
    decimal? BinaryPairingRate,
    decimal? PairUnitBv,
    decimal? FixedPairAmount,
    ProcessingFrequency ProcessingFrequency,
    bool CarryForwardEnabled,
    string QualificationRulesJson,
    string CapRulesJson,
    long ExpectedConfigurationVersion
) : IRequest, IOrganizationAdminRequest;
