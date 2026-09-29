using System.Text.Json;
using modular_mlm.Domain.Services;

namespace modular_mlm.Application.Compensation;

internal static class CommissionRuleJson
{
    public const int MaximumLength = 10_000;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool IsValidQualificationRules(string? json)
    {
        if (!TryReadObject(json, out var document, out var isEmptyRules))
            return false;
        using (document)
        {
            if (isEmptyRules)
                return true;

            try
            {
                var rules = document!.RootElement.Deserialize<QualificationRules>(Options);
                return rules is not null
                    && rules.MinimumPersonalSales >= 0
                    && rules.MinimumPersonalBusinessVolume >= 0
                    && rules.MinimumActiveDirectRecruits >= 0
                    && (rules.RequiredQualificationState?.Length ?? 0) <= 100;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    public static bool IsValidCapRules(string? json)
    {
        if (!TryReadObject(json, out var document, out var isEmptyRules))
            return false;
        using (document)
        {
            if (isEmptyRules)
                return true;

            try
            {
                var rules = document!.RootElement.Deserialize<PairingCapRulesContract>(Options);
                return rules is not null
                    && rules.MaximumAmount >= 0
                    && (!rules.Enabled || rules.MaximumAmount > 0);
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    private static bool TryReadObject(
        string? json,
        out JsonDocument? document,
        out bool isEmptyRules
    )
    {
        document = null;
        isEmptyRules = false;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                isEmptyRules = !document.RootElement.EnumerateObject().Any();
                return true;
            }

            // Preserve the existing [] sentinel used by older draft plans, but
            // reject every non-empty array because runtime processing expects an object.
            if (
                document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.GetArrayLength() == 0
            )
            {
                isEmptyRules = true;
                return true;
            }

            document.Dispose();
            document = null;
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record PairingCapRulesContract(bool Enabled, decimal MaximumAmount);
}
