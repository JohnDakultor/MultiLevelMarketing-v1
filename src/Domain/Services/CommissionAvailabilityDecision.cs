namespace modular_mlm.Domain.Services;

public sealed record CommissionAvailabilityDecision
{
    private CommissionAvailabilityDecision(
        bool isReleasable,
        DateTimeOffset? eligibleAt,
        string reasonCode,
        string reason
    )
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
            throw new ArgumentException("A reason code is required.", nameof(reasonCode));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required.", nameof(reason));

        IsReleasable = isReleasable;
        EligibleAt = eligibleAt;
        ReasonCode = reasonCode;
        Reason = reason;
    }

    public bool IsReleasable { get; }
    public DateTimeOffset? EligibleAt { get; }
    public string ReasonCode { get; }
    public string Reason { get; }

    public static CommissionAvailabilityDecision Releasable(DateTimeOffset eligibleAt) =>
        new(true, eligibleAt, ReasonCodes.Releasable, "The commission is eligible for release.");

    public static CommissionAvailabilityDecision Pending(
        string reasonCode,
        string reason,
        DateTimeOffset? eligibleAt = null
    ) => new(false, eligibleAt, reasonCode, reason);

    public static CommissionAvailabilityDecision Ineligible(string reasonCode, string reason) =>
        new(false, null, reasonCode, reason);

    public static class ReasonCodes
    {
        public const string Releasable = "releasable";
        public const string CommissionNotPending = "commission_not_pending";
        public const string SourceReversed = "source_reversed";
        public const string PaymentNotConfirmed = "payment_not_confirmed";
        public const string OrderNotDelivered = "order_not_delivered";
        public const string ReleaseDelayNotElapsed = "release_delay_not_elapsed";
        public const string UnsupportedReleaseTrigger = "unsupported_release_trigger";
    }
}
