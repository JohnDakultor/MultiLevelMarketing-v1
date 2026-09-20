using modular_mlm.Application.Common.Security;
using modular_mlm.Domain.Compensation;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Application.Compensation.Commands.CreateCommissionPlan;

[Authorize(Roles = Roles.Administrator)]
public sealed record CreateCommissionPlanCommand(
    Guid OrganizationId,
    string Name,
    int Version,
    DateTimeOffset EffectiveFrom,
    decimal DirectSalesRate,
    bool BinaryPairingEnabled,
    decimal? BinaryPairingRate,
    ProcessingFrequency ProcessingFrequency,
    PairingCalculationType PairingCalculationType = PairingCalculationType.PercentageMatchedVolume,
    decimal? PairUnitBv = null,
    decimal? FixedPairAmount = null,
    bool CarryForwardEnabled = true,
    string? QualificationRulesJson = null,
    string? CapRulesJson = null
) : IRequest<Guid>, IOrganizationAdminRequest;
