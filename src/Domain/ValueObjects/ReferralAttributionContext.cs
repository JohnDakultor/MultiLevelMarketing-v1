using modular_mlm.Domain.Exceptions;

namespace modular_mlm.Domain.Referral;

public sealed record ReferralAttributionContext
{
    private ReferralAttributionContext(
        Guid agentId,
        string referralCode,
        DateTimeOffset capturedAt,
        AttributionSource source
    )
    {
        AgentId = agentId;
        ReferralCode = referralCode;
        CapturedAt = capturedAt;
        Source = source;
    }

    public Guid AgentId { get; }
    public string ReferralCode { get; }
    public DateTimeOffset CapturedAt { get; }
    public AttributionSource Source { get; }

    public static ReferralAttributionContext Capture(
        Guid agentId,
        string referralCode,
        DateTimeOffset capturedAt,
        AttributionSource source
    )
    {
        if (agentId == Guid.Empty)
            throw new DomainInvariantException("Attributed agent is required.");

        if (string.IsNullOrWhiteSpace(referralCode))
            throw new DomainInvariantException("Referral code is required.");

        return new ReferralAttributionContext(
            agentId,
            referralCode.Trim().ToUpperInvariant(),
            capturedAt,
            source
        );
    }
}
