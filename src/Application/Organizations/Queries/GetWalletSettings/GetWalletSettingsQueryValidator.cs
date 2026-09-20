namespace modular_mlm.Application.Organizations.Queries.GetWalletSettings;

public sealed class GetWalletSettingsQueryValidator : AbstractValidator<GetWalletSettingsQuery>
{
    public GetWalletSettingsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
    }
}
