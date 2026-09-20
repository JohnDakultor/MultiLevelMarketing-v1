using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using modular_mlm.Application.Common.Interfaces;
using Npgsql;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class PostgresWalletAdjustmentLock(string connectionString) : IWalletAdjustmentLock
{
    public async ValueTask<IAsyncDisposable> AcquireAsync(
        Guid walletId,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        var keyBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes($"wallet-adjustment:{walletId:N}:{idempotencyKey}")
        );
        var lockKey = BinaryPrimitives.ReadInt64LittleEndian(keyBytes);
        var connection = new NpgsqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
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
