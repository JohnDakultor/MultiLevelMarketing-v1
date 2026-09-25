namespace modular_mlm.Application.Common.Interfaces;

public interface IDatabaseExceptionClassifier
{
    bool IsUniqueConstraintViolation(
        DbUpdateException exception,
        string constraintName
    );
}
