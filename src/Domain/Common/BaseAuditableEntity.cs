namespace modular_mlm.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTimeOffset Created { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset LastModified { get; private set; }

    public string? LastModifiedBy { get; private set; }

    public void SetCreationAudit(DateTimeOffset timestamp, string? userId)
    {
        Created = timestamp;
        CreatedBy = userId;
        SetModificationAudit(timestamp, userId);
    }

    public void SetModificationAudit(DateTimeOffset timestamp, string? userId)
    {
        LastModified = timestamp;
        LastModifiedBy = userId;
    }
}
