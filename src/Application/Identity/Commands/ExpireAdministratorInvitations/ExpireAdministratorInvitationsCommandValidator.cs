namespace modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations;

public sealed class ExpireAdministratorInvitationsCommandValidator
    : AbstractValidator<ExpireAdministratorInvitationsCommand>
{
    public ExpireAdministratorInvitationsCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.BatchSize).InclusiveBetween(1, 500);
        RuleFor(x => x.AsOf)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage("AsOf must use UTC.");
    }
}
