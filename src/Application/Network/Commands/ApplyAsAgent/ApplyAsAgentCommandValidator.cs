namespace modular_mlm.Application.Network.Commands.ApplyAsAgent;

public sealed class ApplyAsAgentCommandValidator : AbstractValidator<ApplyAsAgentCommand>
{
    public ApplyAsAgentCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.SponsorAgentId).NotEqual(Guid.Empty).When(x => x.SponsorAgentId.HasValue);
        RuleFor(x => x.SponsorReferralCode)
            .NotEmpty()
            .MaximumLength(64)
            .When(x => x.SponsorReferralCode is not null);
        RuleFor(x => x)
            .Must(x => x.SponsorAgentId is null || string.IsNullOrWhiteSpace(x.SponsorReferralCode))
            .WithMessage("Provide either a Sponsor Agent ID or a Sponsor referral code, not both.");
    }
}
