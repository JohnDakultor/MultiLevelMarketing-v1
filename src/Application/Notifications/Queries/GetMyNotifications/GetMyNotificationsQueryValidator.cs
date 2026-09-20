namespace modular_mlm.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Kind)
            .Must(kind => !kind.HasValue || Enum.IsDefined(kind.Value))
            .WithMessage("Notification kind is invalid.");
    }
}
