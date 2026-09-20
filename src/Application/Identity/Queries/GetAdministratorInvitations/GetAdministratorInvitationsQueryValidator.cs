namespace modular_mlm.Application.Identity.Queries.GetAdministratorInvitations;

public class GetAdministratorInvitationsQueryValidator
    : AbstractValidator<GetAdministratorInvitationsQuery>
{
    public GetAdministratorInvitationsQueryValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum();
    }
}
