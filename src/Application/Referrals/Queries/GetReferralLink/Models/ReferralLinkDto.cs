namespace modular_mlm.Application.Referrals.Queries.GetReferralLink.Models;

public sealed record ReferralLinkDto(string ReferralCode, Guid? ProductId, string RelativeUrl);
