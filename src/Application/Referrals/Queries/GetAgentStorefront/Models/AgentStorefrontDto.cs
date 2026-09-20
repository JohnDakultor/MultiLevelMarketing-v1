namespace modular_mlm.Application.Referrals.Queries.GetAgentStorefront.Models;

public sealed record AgentStorefrontDto(
    Guid AgentId,
    string AgentCode,
    string ReferralCode,
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<StorefrontProductDto> Products
);
