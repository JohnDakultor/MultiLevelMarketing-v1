namespace modular_mlm.Domain.Payouts;

public enum PayoutStatus
{
    Requested,
    UnderReview,
    Approved,
    Processing,
    Paid,
    Rejected,
    Failed,
    Cancelled,
}
