namespace modular_mlm.Application.Organizations.Commands.PublishBranding;

public sealed class PublishBrandingCommandValidator : AbstractValidator<PublishBrandingCommand>
{
    public PublishBrandingCommandValidator()
    {
        RuleFor(command => command.OrganizationId).NotEmpty();
    }
}
