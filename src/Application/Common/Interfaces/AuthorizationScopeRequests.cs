namespace modular_mlm.Application.Common.Interfaces;

public interface IOrganizationAdminRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAdministerOrganizationAsync(OrganizationId, cancellationToken);
}

public interface IOrganizationMemberRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }

    async Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) =>
        await authorizationService.CanAdministerOrganizationAsync(OrganizationId, cancellationToken)
        || await authorizationService.CanAccessCurrentAgentAsync(OrganizationId, cancellationToken)
        || await authorizationService.CanAccessCurrentCustomerAsync(
            OrganizationId,
            cancellationToken
        );
}

public interface IAgentScopedRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid AgentId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAccessAgentAsync(OrganizationId, AgentId, cancellationToken);
}

public interface ICurrentAgentRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAccessCurrentAgentAsync(OrganizationId, cancellationToken);
}

public interface ICurrentAgentOrderRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid OrderId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) =>
        authorizationService.CanAccessAttributedOrderAsync(
            OrganizationId,
            OrderId,
            cancellationToken
        );
}

public interface INullableAgentScopedRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid? AgentId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) =>
        AgentId is { } agentId
            ? authorizationService.CanAccessAgentAsync(OrganizationId, agentId, cancellationToken)
            : authorizationService.CanAdministerOrganizationAsync(
                OrganizationId,
                cancellationToken
            );
}

public interface ICustomerScopedRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid CustomerId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAccessCustomerAsync(OrganizationId, CustomerId, cancellationToken);
}

public interface ICustomerContextRequest
{
    Guid OrganizationId { get; }
}

public interface ICurrentCustomerRequest : ICustomerContextRequest, IAuthorizeRequest
{
    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAccessCurrentCustomerAsync(OrganizationId, cancellationToken);
}

public interface IOrderScopedRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid OrderId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) => authorizationService.CanAccessOrderAsync(OrganizationId, OrderId, cancellationToken);
}

public interface IPayoutScopedRequest : IAuthorizeRequest
{
    Guid OrganizationId { get; }
    Guid PayoutRequestId { get; }

    Task<bool> IAuthorizeRequest.IsAuthorizedAsync(
        IApplicationAuthorizationService authorizationService,
        CancellationToken cancellationToken
    ) =>
        authorizationService.CanAccessPayoutAsync(
            OrganizationId,
            PayoutRequestId,
            cancellationToken
        );
}
