namespace modular_mlm.Application.Common.Interfaces;

public interface ICommissionReleaseLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(
        Guid pendingWalletEntryId,
        CancellationToken cancellationToken
    );
}
