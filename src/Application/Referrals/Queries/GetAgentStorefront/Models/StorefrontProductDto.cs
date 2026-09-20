namespace modular_mlm.Application.Referrals.Queries.GetAgentStorefront.Models;

public sealed record StorefrontProductDto(
    Guid ProductId,
    string Name,
    string Slug,
    string Description,
    string? ImageUrl,
    decimal StartingPrice,
    string ReferralUrl
);
