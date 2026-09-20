using modular_mlm.Domain.Common;
using modular_mlm.Domain.Events;

namespace modular_mlm.Infrastructure.Messaging;

public sealed class OutboxConsumerRegistry
{
    private readonly IReadOnlyDictionary<string, Type> _types;

    public OutboxConsumerRegistry()
    {
        Type[] contracts =
        [
            typeof(AdministratorInvitationAcceptedEvent),
            typeof(AdministratorInvitedEvent),
            typeof(AgentActivatedEvent),
            typeof(AgentPlacedEvent),
            typeof(AgentPlacementMovedEvent),
            typeof(AgentPreferredLegChangedEvent),
            typeof(CommissionCreatedEvent),
            typeof(CommissionReleasedEvent),
            typeof(InventoryAdjustedEvent),
            typeof(InventoryReleasedEvent),
            typeof(InventoryReservationFinalizedEvent),
            typeof(InventoryReservedEvent),
            typeof(NotificationCreatedEvent),
            typeof(NotificationDeliveryFailedEvent),
            typeof(NotificationReadEvent),
            typeof(OrderItemRefundedEvent),
            typeof(OrderPaidEvent),
            typeof(OrderRefundedEvent),
            typeof(PaymentFailedEvent),
            typeof(OrganizationBrandingPublishedEvent),
            typeof(OrganizationDomainConfiguredEvent),
            typeof(OrganizationProvisionedEvent),
            typeof(PayoutApprovedEvent),
            typeof(PayoutCompletedEvent),
            typeof(PayoutFailedEvent),
            typeof(PayoutRequestedEvent),
            typeof(WalletFundsReservedEvent),
        ];
        if (contracts.Any(type => !typeof(BaseEvent).IsAssignableFrom(type)))
            throw new InvalidOperationException("An outbox contract is not a domain event.");
        _types = contracts.ToDictionary(StableName, StringComparer.Ordinal);
    }

    public bool TryResolve(string stableName, out Type messageType) =>
        _types.TryGetValue(stableName, out messageType!);

    public bool IsAllowed(string stableName) => _types.ContainsKey(stableName);

    public static string StableName(Type type) => type.FullName ?? type.Name;
}
