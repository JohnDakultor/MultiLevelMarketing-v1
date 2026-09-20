using modular_mlm.Application.Common.Models;

namespace modular_mlm.Application.Common.Interfaces;

public interface ICurrentIdentityReader
{
    Task<CurrentIdentitySummary?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken
    );
}
