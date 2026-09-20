using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Referral;

public sealed class ReferralAttribution : OrganizationEntity
{
    private ReferralAttribution() { }

    public Guid CustomerId { get; private set; }
    public Guid AgentId { get; private set; }
    public string ReferralCode { get; private set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public AttributionSource Source { get; private set; }

    public static ReferralAttribution Capture(
        Guid organizationId,
        Guid customerId,
        ReferralAttributionContext context,
        DateTimeOffset expiresAt
    )
    {
        if (organizationId == Guid.Empty || customerId == Guid.Empty)
            throw new DomainInvariantException("Organization and customer are required.");
        if (expiresAt <= context.CapturedAt)
            throw new DomainInvariantException("Attribution expiration must follow capture time.");

        return new ReferralAttribution
        {
            OrganizationId = organizationId,
            CustomerId = customerId,
            AgentId = context.AgentId,
            ReferralCode = context.ReferralCode,
            CapturedAt = context.CapturedAt,
            ExpiresAt = expiresAt,
            Source = context.Source,
        };
    }

    public void Recapture(ReferralAttributionContext context, DateTimeOffset expiresAt)
    {
        if (expiresAt <= context.CapturedAt)
            throw new DomainInvariantException("Attribution expiration must follow capture time.");

        AgentId = context.AgentId;
        ReferralCode = context.ReferralCode;
        CapturedAt = context.CapturedAt;
        ExpiresAt = expiresAt;
        Source = context.Source;
    }

    public ReferralAttributionContext ToContext() =>
        ReferralAttributionContext.Capture(AgentId, ReferralCode, CapturedAt, Source);
}
