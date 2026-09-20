using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Catalog;

public sealed class ProductCommissionProfile : OrganizationEntity
{
    private ProductCommissionProfile() { }

    public string Name { get; private set; } = string.Empty;
    public bool DirectSalesEligible { get; private set; }
    public decimal? DirectSalesRateOverride { get; private set; }
    public bool BinaryVolumeEligible { get; private set; }
    public decimal? BinaryVolumeOverride { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }

    public static ProductCommissionProfile Create(
        Guid organizationId,
        string name,
        DateTimeOffset effectiveFrom
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(name))
            throw new DomainInvariantException("Organization and profile name are required.");
        return new ProductCommissionProfile
        {
            OrganizationId = organizationId,
            Name = name.Trim(),
            EffectiveFrom = effectiveFrom,
            DirectSalesEligible = true,
            BinaryVolumeEligible = true,
        };
    }

    public void ConfigureDirectSales(bool eligible, decimal? rateOverride)
    {
        if (rateOverride is < 0 or > 1)
            throw new DomainInvariantException("Rate must be between zero and one.");
        DirectSalesEligible = eligible;
        DirectSalesRateOverride = eligible ? rateOverride : null;
    }

    public void ConfigureBinaryVolume(bool eligible, decimal? volumeOverride)
    {
        if (volumeOverride < 0)
            throw new DomainInvariantException("BV cannot be negative.");
        BinaryVolumeEligible = eligible;
        BinaryVolumeOverride = eligible ? volumeOverride : null;
    }

    public void End(DateTimeOffset effectiveTo)
    {
        if (effectiveTo <= EffectiveFrom)
            throw new DomainInvariantException("End must be after start.");
        EffectiveTo = effectiveTo;
    }
}
