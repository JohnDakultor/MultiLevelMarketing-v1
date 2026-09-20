using Microsoft.EntityFrameworkCore;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Constants;

namespace modular_mlm.Infrastructure.Services;

public sealed class ApplicationAuthorizationService(
    IUser currentUser,
    IApplicationDbContext db,
    IAdministratorAccountService administratorAccounts
) : IApplicationAuthorizationService
{
    public Task<bool> CanProvisionOrganizationAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(
            !string.IsNullOrWhiteSpace(currentUser.Id)
                && currentUser.Roles?.Contains(Roles.PlatformAdministrator) == true
        );

    public async Task<bool> CanAdministerOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        // Platform administrators provision and bootstrap organizations before a
        // tenant administrator account exists. Their authority is global by role;
        // ordinary administrators remain bound to their assigned organization.
        if (currentUser.Roles?.Contains(Roles.PlatformAdministrator) == true)
            return true;

        return await administratorAccounts.CanManageOrganizationAsync(
            currentUser.Id,
            organizationId,
            cancellationToken
        );
    }

    public async Task<bool> CanAccessCustomerAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default
    )
    {
        if (await CanAdministerOrganizationAsync(organizationId, cancellationToken))
            return true;
        if (customerId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        return await db.CustomerProfiles.AnyAsync(
            customer =>
                customer.Id == customerId
                && customer.OrganizationId == organizationId
                && customer.UserId == currentUser.Id,
            cancellationToken
        );
    }

    public async Task<bool> CanAccessCurrentCustomerAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        return await db.CustomerProfiles.AnyAsync(
            customer =>
                customer.OrganizationId == organizationId && customer.UserId == currentUser.Id,
            cancellationToken
        );
    }

    public async Task<bool> CanAccessAgentAsync(
        Guid organizationId,
        Guid agentId,
        CancellationToken cancellationToken = default
    )
    {
        if (await CanAdministerOrganizationAsync(organizationId, cancellationToken))
            return true;
        if (agentId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        return await db.Agents.AnyAsync(
            agent =>
                agent.Id == agentId
                && agent.OrganizationId == organizationId
                && agent.UserId == currentUser.Id,
            cancellationToken
        );
    }

    public Task<bool> CanAccessCurrentAgentAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        if (organizationId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return Task.FromResult(false);

        return db.Agents.AnyAsync(
            agent => agent.OrganizationId == organizationId && agent.UserId == currentUser.Id,
            cancellationToken
        );
    }

    public async Task<bool> CanAccessAttributedOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken = default
    )
    {
        if (
            organizationId == Guid.Empty
            || orderId == Guid.Empty
            || string.IsNullOrWhiteSpace(currentUser.Id)
        )
            return false;

        return await (
            from order in db.Orders
            join agent in db.Agents on order.AttributedAgentId equals agent.Id
            where
                order.Id == orderId
                && order.OrganizationId == organizationId
                && agent.OrganizationId == organizationId
                && agent.UserId == currentUser.Id
            select order.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> CanAccessOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken = default
    )
    {
        if (await CanAdministerOrganizationAsync(organizationId, cancellationToken))
            return true;
        if (orderId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        return await (
            from order in db.Orders
            join customer in db.CustomerProfiles on order.CustomerId equals customer.Id
            where
                order.Id == orderId
                && order.OrganizationId == organizationId
                && customer.OrganizationId == organizationId
                && customer.UserId == currentUser.Id
            select order.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> CanAccessPayoutAsync(
        Guid organizationId,
        Guid payoutRequestId,
        CancellationToken cancellationToken = default
    )
    {
        if (await CanAdministerOrganizationAsync(organizationId, cancellationToken))
            return true;
        if (payoutRequestId == Guid.Empty || string.IsNullOrWhiteSpace(currentUser.Id))
            return false;

        return await (
            from payout in db.PayoutRequests
            join agent in db.Agents on payout.AgentId equals agent.Id
            where
                payout.Id == payoutRequestId
                && payout.OrganizationId == organizationId
                && agent.OrganizationId == organizationId
                && agent.UserId == currentUser.Id
            select payout.Id
        ).AnyAsync(cancellationToken);
    }

    public async Task<bool> CanAgentInspectDownlineAsync(
        Guid organizationId,
        Guid agentId,
        Guid targetMemberId,
        CancellationToken cancellationToken = default
    )
    {
        if (await CanAdministerOrganizationAsync(organizationId, cancellationToken))
            return true;
        if (!await CanAccessAgentAsync(organizationId, agentId, cancellationToken))
            return false;
        if (targetMemberId == agentId)
            return true;

        return await db.PlacementClosures.AnyAsync(
            node =>
                node.OrganizationId == organizationId
                && node.AncestorAgentId == agentId
                && node.DescendantAgentId == targetMemberId,
            cancellationToken
        );
    }
}
