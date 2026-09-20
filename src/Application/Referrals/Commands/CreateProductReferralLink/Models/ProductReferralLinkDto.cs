namespace modular_mlm.Application.Referrals.Commands.CreateProductReferralLink.Models;

public sealed record ProductReferralLinkDto(
    Guid ProductId,
    string ProductSlug,
    string ReferralCode,
    string CanonicalUrl,
    string QrPayload
);
