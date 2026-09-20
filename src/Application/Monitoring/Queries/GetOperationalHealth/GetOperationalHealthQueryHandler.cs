using modular_mlm.Application.Common.Exceptions;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Monitoring.Queries.GetOperationalHealth.Models;

namespace modular_mlm.Application.Monitoring.Queries.GetOperationalHealth;

public sealed class GetOperationalHealthQueryHandler(
    IUser currentUser,
    IAdministratorAccountService administratorAccounts,
    IOperationalHealthReader reader,
    TimeProvider clock
) : IRequestHandler<GetOperationalHealthQuery, OperationalHealthDto>
{
    public async Task<OperationalHealthDto> Handle(
        GetOperationalHealthQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUser.Id ?? throw new UnauthorizedAccessException();
        if (
            !await administratorAccounts.CanManageOrganizationAsync(
                userId,
                request.OrganizationId,
                cancellationToken
            )
        )
            throw new ForbiddenAccessException();

        var now = clock.GetUtcNow();
        var snapshot = await reader.ReadAsync(request.OrganizationId, now, cancellationToken);
        return new OperationalHealthDto(
            request.OrganizationId,
            now,
            snapshot.PendingOutboxMessages,
            snapshot.OldestPendingOutboxAgeSeconds,
            snapshot.FailedWebhookAttempts,
            snapshot.PaymentsAwaitingReconciliation,
            snapshot.PayoutsAwaitingProviderCompletion,
            snapshot.CompensationBacklog,
            snapshot.RecentPairingFailures,
            snapshot.LatestPairingDurationMilliseconds
        );
    }
}
