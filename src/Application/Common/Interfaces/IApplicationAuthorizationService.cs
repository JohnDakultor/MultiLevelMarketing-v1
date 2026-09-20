namespace modular_mlm.Application.Common.Interfaces;

public interface IApplicationAuthorizationService
{
    Task<bool> CanProvisionOrganizationAsync(CancellationToken cancellationToken = default);

    Task<bool> CanAdministerOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessCustomerAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessCurrentCustomerAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessAgentAsync(
        Guid organizationId,
        Guid agentId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessCurrentAgentAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessAttributedOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessOrderAsync(
        Guid organizationId,
        Guid orderId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAccessPayoutAsync(
        Guid organizationId,
        Guid payoutRequestId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanAgentInspectDownlineAsync(
        Guid organizationId,
        Guid agentId,
        Guid targetMemberId,
        CancellationToken cancellationToken = default
    );
}
