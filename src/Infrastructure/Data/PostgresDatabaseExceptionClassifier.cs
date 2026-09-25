using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using Npgsql;

namespace modular_mlm.Infrastructure.Data;

public sealed class PostgresDatabaseExceptionClassifier : IDatabaseExceptionClassifier
{
    public bool IsUniqueConstraintViolation(
        DbUpdateException exception,
        string constraintName
    )
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(constraintName);

        return FindPostgresException(exception) is
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: var actualConstraintName,
        } && string.Equals(actualConstraintName, constraintName, StringComparison.Ordinal);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
                return postgresException;
        }

        return null;
    }
}
