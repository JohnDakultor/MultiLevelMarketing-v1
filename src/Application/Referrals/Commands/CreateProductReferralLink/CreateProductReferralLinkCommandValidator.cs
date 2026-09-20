namespace modular_mlm.Application.Referrals.Commands.CreateProductReferralLink;

public sealed class CreateProductReferralLinkCommandValidator
    : AbstractValidator<CreateProductReferralLinkCommand>
{
    public CreateProductReferralLinkCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
