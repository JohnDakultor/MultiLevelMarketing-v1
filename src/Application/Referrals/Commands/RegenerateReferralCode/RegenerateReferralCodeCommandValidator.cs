namespace modular_mlm.Application.Referrals.Commands.RegenerateReferralCode;

public sealed class RegenerateReferralCodeCommandValidator
    : AbstractValidator<RegenerateReferralCodeCommand>
{
    public RegenerateReferralCodeCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.AgentId).NotEmpty();
    }
}
