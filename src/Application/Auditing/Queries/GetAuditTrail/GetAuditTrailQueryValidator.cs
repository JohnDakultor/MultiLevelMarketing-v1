namespace modular_mlm.Application.Auditing.Queries.GetAuditTrail;

public sealed class GetAuditTrailQueryValidator : AbstractValidator<GetAuditTrailQuery>
{
    public GetAuditTrailQueryValidator()
    {
        RuleFor(query => query.OrganizationId).NotEmpty();
        RuleFor(query => query.ActorUserId).NotEmpty().When(query => query.ActorUserId.HasValue);
        RuleFor(query => query.EntityId).NotEmpty().When(query => query.EntityId.HasValue);
        RuleFor(query => query.Action).MaximumLength(100);
        RuleFor(query => query.EntityType).MaximumLength(200);
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From)
            .When(query => query.From.HasValue && query.To.HasValue);
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
