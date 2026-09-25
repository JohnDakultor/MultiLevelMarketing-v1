using modular_mlm.Domain.Compensation;

namespace modular_mlm.Application.Compensation.Queries.GetCommissionPlans.Models;

public sealed record CommissionPlanDto(
    Guid Id,
    string Name,
    int Version,
    CommissionPlanStatus Status,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    decimal DirectSalesRate,
    bool BinaryPairingEnabled,
    decimal? BinaryPairingRate,
    ProcessingFrequency ProcessingFrequency,
    PairingCalculationType PairingCalculationType,
    decimal? PairUnitBv,
    decimal? FixedPairAmount,
    bool CarryForwardEnabled,
    string QualificationRulesJson,
    string CapRulesJson,
    long ConfigurationVersion
);
