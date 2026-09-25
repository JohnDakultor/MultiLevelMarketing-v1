using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Compensation;

public sealed class CommissionPlan : OrganizationEntity
{
    private CommissionPlan() { }

    public string Name { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public long ConfigurationVersion { get; private set; }
    public CommissionPlanStatus Status { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public decimal DirectSalesRate { get; private set; }
    public BinaryPairingRule BinaryPairing { get; private set; } = null!;
    public string QualificationRulesJson { get; private set; } = "[]";
    public string CapRulesJson { get; private set; } = "[]";

    public static CommissionPlan Draft(
        Guid organizationId,
        string name,
        int version,
        DateTimeOffset effectiveFrom
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(name) || version < 1)
            throw new DomainInvariantException("Plan identity, name, and version are required.");
        return new CommissionPlan
        {
            OrganizationId = organizationId,
            Name = name.Trim(),
            Version = version,
            ConfigurationVersion = 0,
            EffectiveFrom = effectiveFrom,
            Status = CommissionPlanStatus.Draft,
            BinaryPairing = BinaryPairingRule.Disabled(),
        };
    }

    public void ConfigureDirectSales(decimal rate)
    {
        EnsureDraft();
        if (rate is < 0 or > 1)
            throw new DomainInvariantException("Direct sales rate must be between zero and one.");
        DirectSalesRate = rate;
    }

    public void ConfigureBinaryPairing(BinaryPairingRule rule)
    {
        EnsureDraft();
        BinaryPairing = rule ?? throw new ArgumentNullException(nameof(rule));
    }

    public void ConfigureQualificationRules(string rulesJson)
    {
        EnsureDraft();
        QualificationRulesJson = RequireJson(rulesJson, "Qualification rules");
    }

    public void ConfigureCapRules(string rulesJson)
    {
        EnsureDraft();
        CapRulesJson = RequireJson(rulesJson, "Cap rules");
    }

    public void AdvanceConfigurationVersion()
    {
        EnsureDraft();
        ConfigurationVersion = checked(ConfigurationVersion + 1);
    }

    public void Publish()
    {
        EnsureDraft();
        Status = CommissionPlanStatus.Active;
        ConfigurationVersion = checked(ConfigurationVersion + 1);
    }

    public void Retire(DateTimeOffset effectiveTo)
    {
        if (Status != CommissionPlanStatus.Active || effectiveTo <= EffectiveFrom)
            throw new DomainInvariantException(
                "Only an active plan can be retired after its start."
            );
        EffectiveTo = effectiveTo;
        Status = CommissionPlanStatus.Retired;
        ConfigurationVersion = checked(ConfigurationVersion + 1);
    }

    private void EnsureDraft()
    {
        if (Status != CommissionPlanStatus.Draft)
            throw new DomainInvariantException("Published plan versions are immutable.");
    }

    private static string RequireJson(string value, string name) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainInvariantException($"{name} JSON is required.")
            : value.Trim();
}
