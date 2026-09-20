namespace modular_mlm.Application.Referrals.Queries.GetReferralDashboard.Models;

public sealed record ReferralDashboardDto(
    Guid AgentId,
    string ReferralCode,
    string StorefrontUrl,
    int DirectRecruitCount,
    int AttributedOrderCount,
    decimal AttributedSales,
    decimal PendingCommission,
    decimal AvailableCommission
);
