using System.Collections.ObjectModel;

namespace modular_mlm.Application.Common.Auditing;

public sealed record AuditCoverageDefinition(
    string Action,
    string EntityType,
    bool RequiresReason = false
);

public static class AuditCoverageMap
{
    public static readonly AuditCoverageDefinition AgentApproved = Define(
        AuditActionNames.AgentApproved,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentRejected = Define(
        AuditActionNames.AgentRejected,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentActivated = Define(
        AuditActionNames.AgentActivated,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentSuspended = Define(
        AuditActionNames.AgentSuspended,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentReactivated = Define(
        AuditActionNames.AgentReactivated,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentPlaced = Define(
        AuditActionNames.AgentPlaced,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentAutoPlaced = Define(
        AuditActionNames.AgentAutoPlaced,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentPreferredLegChanged = Define(
        AuditActionNames.AgentPreferredLegChanged,
        AuditEntityNames.Agent
    );
    public static readonly AuditCoverageDefinition AgentPlacementMoved = Define(
        AuditActionNames.AgentPlacementMoved,
        AuditEntityNames.Agent,
        true
    );
    public static readonly AuditCoverageDefinition ReferralCodeRegenerated = Define(
        AuditActionNames.ReferralCodeRegenerated,
        AuditEntityNames.Agent
    );

    public static readonly AuditCoverageDefinition ProductCreated = Define(
        AuditActionNames.ProductCreated,
        AuditEntityNames.Product
    );
    public static readonly AuditCoverageDefinition ProductUpdated = Define(
        AuditActionNames.ProductUpdated,
        AuditEntityNames.Product
    );
    public static readonly AuditCoverageDefinition ProductVariantCreated = Define(
        AuditActionNames.ProductVariantCreated,
        AuditEntityNames.ProductVariant
    );

    public static readonly AuditCoverageDefinition ProductVariantUpdated = Define(
        AuditActionNames.ProductVariantUpdated,
        AuditEntityNames.ProductVariant
    );
    public static readonly AuditCoverageDefinition ProductVariantArchived = Define(
        AuditActionNames.ProductVariantArchived,
        AuditEntityNames.ProductVariant,
        true
    );

    public static readonly AuditCoverageDefinition ProductPublished = Define(
        AuditActionNames.ProductPublished,
        AuditEntityNames.Product
    );
    public static readonly AuditCoverageDefinition ProductArchived = Define(
        AuditActionNames.ProductArchived,
        AuditEntityNames.Product
    );
    public static readonly AuditCoverageDefinition CommissionProfileCreated = Define(
        AuditActionNames.CommissionProfileCreated,
        AuditEntityNames.ProductCommissionProfile
    );
    public static readonly AuditCoverageDefinition CommissionProfileAssigned = Define(
        AuditActionNames.CommissionProfileAssigned,
        AuditEntityNames.Product
    );
    public static readonly AuditCoverageDefinition InventoryAdjusted = Define(
        AuditActionNames.InventoryAdjusted,
        AuditEntityNames.InventoryAdjustment,
        true
    );

    public static readonly AuditCoverageDefinition OrganizationCreated = Define(
        AuditActionNames.OrganizationCreated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition OrganizationProfileUpdated = Define(
        AuditActionNames.OrganizationProfileUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition BrandingPublished = Define(
        AuditActionNames.BrandingPublished,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition BrandingAssetUploaded = Define(
        AuditActionNames.BrandingAssetUploaded,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition OrganizationDomainConfigured = Define(
        AuditActionNames.OrganizationDomainConfigured,
        AuditEntityNames.OrganizationDomain
    );
    public static readonly AuditCoverageDefinition OrganizationDomainRemoved = Define(
        AuditActionNames.OrganizationDomainRemoved,
        AuditEntityNames.OrganizationDomain
    );
    public static readonly AuditCoverageDefinition BrandingUpdated = Define(
        AuditActionNames.BrandingUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition FeaturesUpdated = Define(
        AuditActionNames.FeaturesUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition NetworkSettingsUpdated = Define(
        AuditActionNames.NetworkSettingsUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition ReferralSettingsUpdated = Define(
        AuditActionNames.ReferralSettingsUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition WalletSettingsUpdated = Define(
        AuditActionNames.WalletSettingsUpdated,
        AuditEntityNames.Organization
    );
    public static readonly AuditCoverageDefinition CommerceSettingsUpdated = Define(
        AuditActionNames.CommerceSettingsUpdated,
        AuditEntityNames.Organization
    );

    public static readonly AuditCoverageDefinition CommissionPlanCreated = Define(
        AuditActionNames.CommissionPlanCreated,
        AuditEntityNames.CommissionPlan
    );
    public static readonly AuditCoverageDefinition CommissionPlanPublished = Define(
        AuditActionNames.CommissionPlanPublished,
        AuditEntityNames.CommissionPlan
    );
    public static readonly AuditCoverageDefinition CommissionPlanRetired = Define(
        AuditActionNames.CommissionPlanRetired,
        AuditEntityNames.CommissionPlan,
        true
    );
    public static readonly AuditCoverageDefinition WalletAdjusted = Define(
        AuditActionNames.WalletAdjusted,
        AuditEntityNames.WalletEntry,
        true
    );
    public static readonly AuditCoverageDefinition OrderItemRefundRequested = Define(
        AuditActionNames.OrderItemRefundRequested,
        AuditEntityNames.OrderItemRefund,
        true
    );
    public static readonly AuditCoverageDefinition OrderItemRefundReversed = Define(
        AuditActionNames.OrderItemRefundReversed,
        AuditEntityNames.OrderItemRefund
    );
    public static readonly AuditCoverageDefinition OrderCancelled = Define(
        AuditActionNames.OrderCancelled,
        AuditEntityNames.Order,
        true
    );
    public static readonly AuditCoverageDefinition PayoutApproved = Define(
        AuditActionNames.PayoutApproved,
        AuditEntityNames.PayoutRequest
    );
    public static readonly AuditCoverageDefinition PayoutRejected = Define(
        AuditActionNames.PayoutRejected,
        AuditEntityNames.PayoutRequest
    );
    public static readonly AuditCoverageDefinition PayoutProcessingStarted = Define(
        AuditActionNames.PayoutProcessingStarted,
        AuditEntityNames.PayoutRequest
    );
    public static readonly AuditCoverageDefinition PayoutCompleted = Define(
        AuditActionNames.PayoutCompleted,
        AuditEntityNames.PayoutRequest
    );
    public static readonly AuditCoverageDefinition PayoutFailed = Define(
        AuditActionNames.PayoutFailed,
        AuditEntityNames.PayoutRequest
    );
    public static readonly AuditCoverageDefinition PayoutAccountVerified = Define(
        AuditActionNames.PayoutAccountVerified,
        AuditEntityNames.PayoutAccount
    );
    public static readonly AuditCoverageDefinition PayoutAccountRejected = Define(
        AuditActionNames.PayoutAccountRejected,
        AuditEntityNames.PayoutAccount
    );

    public static readonly AuditCoverageDefinition AdministratorInvitationCreated = Define(
        AuditActionNames.AdministratorInvitationCreated,
        AuditEntityNames.AdministratorInvitation
    );
    public static readonly AuditCoverageDefinition AdministratorInvitationAccepted = Define(
        AuditActionNames.AdministratorInvitationAccepted,
        AuditEntityNames.AdministratorInvitation
    );
    public static readonly AuditCoverageDefinition AdministratorInvitationRevoked = Define(
        AuditActionNames.AdministratorInvitationRevoked,
        AuditEntityNames.AdministratorInvitation,
        true
    );
    public static readonly AuditCoverageDefinition AdministratorInvitationExpired = Define(
        AuditActionNames.AdministratorInvitationExpired,
        AuditEntityNames.AdministratorInvitation
    );
    public static readonly AuditCoverageDefinition AdministratorRoleGranted = Define(
        AuditActionNames.AdministratorRoleGranted,
        AuditEntityNames.AdministratorAccount,
        true
    );
    public static readonly AuditCoverageDefinition AdministratorRoleRevoked = Define(
        AuditActionNames.AdministratorRoleRevoked,
        AuditEntityNames.AdministratorAccount,
        true
    );
    public static readonly AuditCoverageDefinition AdministratorLoginSucceeded = Define(
        AuditActionNames.AdministratorLoginSucceeded,
        AuditEntityNames.AdministratorAccount
    );
    public static readonly AuditCoverageDefinition AdministratorLoginFailed = Define(
        AuditActionNames.AdministratorLoginFailed,
        AuditEntityNames.AdministratorAccount
    );
    public static readonly AuditCoverageDefinition AdministratorLockedOut = Define(
        AuditActionNames.AdministratorLockedOut,
        AuditEntityNames.AdministratorAccount
    );
    public static readonly AuditCoverageDefinition AdministratorPasswordChanged = Define(
        AuditActionNames.AdministratorPasswordChanged,
        AuditEntityNames.AdministratorAccount
    );
    public static readonly AuditCoverageDefinition AdministratorTwoFactorChanged = Define(
        AuditActionNames.AdministratorTwoFactorChanged,
        AuditEntityNames.AdministratorAccount
    );
    public static readonly AuditCoverageDefinition OutboxMessageReplayed = Define(
        AuditActionNames.OutboxMessageReplayed,
        AuditEntityNames.OutboxMessage,
        true
    );

    private static readonly ReadOnlyCollection<AuditCoverageDefinition> Definitions =
        Array.AsReadOnly(
            new[]
            {
                AgentApproved,
                AgentRejected,
                AgentActivated,
                AgentSuspended,
                AgentReactivated,
                AgentPlaced,
                AgentAutoPlaced,
                AgentPreferredLegChanged,
                AgentPlacementMoved,
                ReferralCodeRegenerated,
                ProductCreated,
                ProductVariantCreated,
                ProductVariantUpdated,
                ProductVariantArchived,
                ProductUpdated,
                ProductPublished,
                ProductArchived,
                CommissionProfileCreated,
                CommissionProfileAssigned,
                InventoryAdjusted,
                OrganizationCreated,
                OrganizationProfileUpdated,
                BrandingPublished,
                BrandingAssetUploaded,
                OrganizationDomainConfigured,
                OrganizationDomainRemoved,
                BrandingUpdated,
                FeaturesUpdated,
                NetworkSettingsUpdated,
                ReferralSettingsUpdated,
                WalletSettingsUpdated,
                CommerceSettingsUpdated,
                CommissionPlanCreated,
                CommissionPlanPublished,
                CommissionPlanRetired,
                WalletAdjusted,
                OrderItemRefundRequested,
                OrderItemRefundReversed,
                OrderCancelled,
                PayoutApproved,
                PayoutRejected,
                PayoutProcessingStarted,
                PayoutCompleted,
                PayoutFailed,
                PayoutAccountVerified,
                PayoutAccountRejected,
                AdministratorInvitationCreated,
                AdministratorInvitationAccepted,
                AdministratorInvitationRevoked,
                AdministratorInvitationExpired,
                AdministratorRoleGranted,
                AdministratorRoleRevoked,
                AdministratorLoginSucceeded,
                AdministratorLoginFailed,
                AdministratorLockedOut,
                AdministratorPasswordChanged,
                AdministratorTwoFactorChanged,
                OutboxMessageReplayed,
            }
        );

    private static readonly IReadOnlyDictionary<
        string,
        AuditCoverageDefinition
    > DefinitionsByAction = new ReadOnlyDictionary<string, AuditCoverageDefinition>(
        Definitions.ToDictionary(definition => definition.Action, StringComparer.Ordinal)
    );

    public static IReadOnlyList<AuditCoverageDefinition> All => Definitions;

    public static AuditCoverageDefinition GetByAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Audit action is required.", nameof(action));

        return DefinitionsByAction.TryGetValue(action.Trim(), out var definition)
            ? definition
            : throw new KeyNotFoundException($"Audit action '{action}' is not registered.");
    }

    private static AuditCoverageDefinition Define(
        string action,
        string entityType,
        bool requiresReason = false
    ) => new(action, entityType, requiresReason);
}
