namespace modular_mlm.Domain.Common;

public abstract class OrganizationEntity : BaseAuditableEntity
{
    public Guid OrganizationId { get; protected set; }
}
