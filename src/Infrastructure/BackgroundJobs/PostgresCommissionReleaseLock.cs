using System.Buffers.Binary;
using modular_mlm.Application.Common.Interfaces;
using Npgsql;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class PostgresCommissionReleaseLock(string connectionString) : ICommissionReleaseLock
{
    public async ValueTask<IAsyncDisposable> AcquireAsync(
        Guid pendingWalletEntryId,
        CancellationToken cancellationToken
    )
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            var lockKey = BinaryPrimitives.ReadInt64LittleEndian(
                pendingWalletEntryId.ToByteArray()
            );
            await using var command = new NpgsqlCommand(
                "SELECT pg_advisory_lock(@lockKey);",
                connection
            );
            command.Parameters.AddWithValue("lockKey", lockKey);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return new AdvisoryLockLease(connection, lockKey);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class AdvisoryLockLease(NpgsqlConnection connection, long lockKey)
        : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_advisory_unlock(@lockKey);",
                    connection
                );
                command.Parameters.AddWithValue("lockKey", lockKey);
                await command.ExecuteNonQueryAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
