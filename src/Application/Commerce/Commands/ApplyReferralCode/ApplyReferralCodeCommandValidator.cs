namespace modular_mlm.Application.Commerce.Commands.ApplyReferralCode;

public sealed class ApplyReferralCodeCommandValidator : AbstractValidator<ApplyReferralCodeCommand>
{
    public ApplyReferralCodeCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ReferralCode).NotEmpty().MaximumLength(64);
    }
}
