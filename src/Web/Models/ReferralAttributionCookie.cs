using modular_mlm.Domain.Referral;

namespace modular_mlm.Web.Models;

public sealed record ReferralAttributionCookie(
    Guid OrganizationId,
    string ReferralCode,
    DateTimeOffset CapturedAt,
    AttributionSource Source
)
{
    public static string GetName(Guid organizationId) => $"mlm_referral_{organizationId:N}";
}
