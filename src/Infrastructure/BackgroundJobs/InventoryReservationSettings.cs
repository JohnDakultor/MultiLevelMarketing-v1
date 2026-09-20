using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class InventoryReservationSettings(
    IOptions<InventoryReservationCleanupOptions> options
) : IInventoryReservationSettings
{
    public TimeSpan Lifetime => TimeSpan.FromMinutes(options.Value.ReservationLifetimeMinutes);
}
