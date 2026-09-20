namespace modular_mlm.Application.Common.Interfaces;

public interface IWalletAdjustmentLock
{
    ValueTask<IAsyncDisposable> AcquireAsync(
        Guid walletId,
        string idempotencyKey,
        CancellationToken cancellationToken
    );
}
