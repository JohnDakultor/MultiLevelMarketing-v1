namespace modular_mlm.Application.Catalog.Queries.GetProductCommissionProfiles.Models;

public sealed record ProductCommissionProfileDto(
    Guid Id,
    string Name,
    bool DirectSalesEligible,
    decimal? DirectSalesRateOverride,
    bool BinaryVolumeEligible,
    decimal? BinaryVolumeOverride,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo
);
