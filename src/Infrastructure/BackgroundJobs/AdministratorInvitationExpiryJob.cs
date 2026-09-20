using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Application.Identity.Commands.ExpireAdministratorInvitations;

namespace modular_mlm.Infrastructure.BackgroundJobs;

public sealed class AdministratorInvitationExpiryJob(
    IApplicationDbContext db,
    ISender sender,
    IOptions<AdministratorInvitationExpiryOptions> options,
    TimeProvider clock,
    ILogger<AdministratorInvitationExpiryJob> logger
)
{
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var organizationIds = await db
            .AdministratorInvitations.AsNoTracking()
            .Where(x =>
                x.Status == Domain.Organizations.AdministratorInvitationStatus.Pending
                && x.ExpiresAt <= now
            )
            .Select(x => x.OrganizationId)
            .Distinct()
            .OrderBy(x => x)
            .Take(options.Value.OrganizationBatchSize)
            .ToListAsync(cancellationToken);
        var expired = 0;
        foreach (var organizationId in organizationIds)
        {
            try
            {
                var result = await sender.Send(
                    new ExpireAdministratorInvitationsCommand(
                        organizationId,
                        options.Value.InvitationBatchSize,
                        now
                    ),
                    cancellationToken
                );
                expired += result.ExpiredCount;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Administrator invitation expiry failed for {OrganizationId}",
                    organizationId
                );
            }
        }
        return expired;
    }
}
