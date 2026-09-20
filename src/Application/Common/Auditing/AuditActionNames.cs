namespace modular_mlm.Application.Common.Auditing;

public static class AuditActionNames
{
    public const string AgentApproved = "AGENT_APPROVED";
    public const string AgentRejected = "AGENT_REJECTED";
    public const string AgentActivated = "AGENT_ACTIVATED";
    public const string AgentSuspended = "AGENT_SUSPENDED";
    public const string AgentReactivated = "AGENT_REACTIVATED";
    public const string AgentPlaced = "AGENT_PLACED";
    public const string AgentAutoPlaced = "AGENT_AUTO_PLACED";
    public const string AgentPreferredLegChanged = "AGENT_PREFERRED_LEG_CHANGED";
    public const string AgentPlacementMoved = "AGENT_PLACEMENT_MOVED";
    public const string ReferralCodeRegenerated = "REFERRAL_CODE_REGENERATED";

    public const string ProductCreated = "PRODUCT_CREATED";
    public const string ProductVariantCreated = "PRODUCT_VARIANT_CREATED";
    public const string ProductVariantUpdated = "PRODUCT_VARIANT_UPDATED";
    public const string ProductVariantArchived = "PRODUCT_VARIANT_ARCHIVED";
    public const string ProductUpdated = "PRODUCT_UPDATED";
    public const string ProductPublished = "PRODUCT_PUBLISHED";
    public const string ProductArchived = "PRODUCT_ARCHIVED";
    public const string CommissionProfileCreated = "COMMISSION_PROFILE_CREATED";
    public const string CommissionProfileAssigned = "COMMISSION_PROFILE_ASSIGNED";
    public const string InventoryAdjusted = "INVENTORY_ADJUSTED";

    public const string OrganizationCreated = "ORGANIZATION_CREATED";
    public const string OrganizationProfileUpdated = "ORGANIZATION_PROFILE_UPDATED";
    public const string BrandingPublished = "BRANDING_PUBLISHED";
    public const string BrandingAssetUploaded = "BRANDING_ASSET_UPLOADED";
    public const string OrganizationDomainConfigured = "ORGANIZATION_DOMAIN_CONFIGURED";
    public const string OrganizationDomainRemoved = "ORGANIZATION_DOMAIN_REMOVED";
    public const string BrandingUpdated = "BRANDING_UPDATED";
    public const string FeaturesUpdated = "FEATURES_UPDATED";
    public const string NetworkSettingsUpdated = "NETWORK_SETTINGS_UPDATED";
    public const string ReferralSettingsUpdated = "REFERRAL_SETTINGS_UPDATED";
    public const string WalletSettingsUpdated = "WALLET_SETTINGS_UPDATED";
    public const string CommerceSettingsUpdated = "COMMERCE_SETTINGS_UPDATED";

    public const string CommissionPlanCreated = "COMMISSION_PLAN_CREATED";
    public const string CommissionPlanPublished = "COMMISSION_PLAN_PUBLISHED";
    public const string CommissionPlanRetired = "COMMISSION_PLAN_RETIRED";
    public const string WalletAdjusted = "WALLET_ADJUSTED";
    public const string OrderItemRefundRequested = "ORDER_ITEM_REFUND_REQUESTED";
    public const string OrderItemRefundReversed = "ORDER_ITEM_REFUND_REVERSED";
    public const string OrderCancelled = "ORDER_CANCELLED";
    public const string PayoutApproved = "PAYOUT_APPROVED";
    public const string PayoutRejected = "PAYOUT_REJECTED";
    public const string PayoutProcessingStarted = "PAYOUT_PROCESSING_STARTED";
    public const string PayoutCompleted = "PAYOUT_COMPLETED";
    public const string PayoutFailed = "PAYOUT_FAILED";
    public const string PayoutAccountVerified = "PAYOUT_ACCOUNT_VERIFIED";
    public const string PayoutAccountRejected = "PAYOUT_ACCOUNT_REJECTED";

    public const string AdministratorInvitationCreated = "ADMIN_INVITATION_CREATED";
    public const string AdministratorInvitationAccepted = "ADMIN_INVITATION_ACCEPTED";
    public const string AdministratorInvitationRevoked = "ADMIN_INVITATION_REVOKED";
    public const string AdministratorInvitationExpired = "ADMIN_INVITATION_EXPIRED";
    public const string AdministratorRoleGranted = "ADMIN_ROLE_GRANTED";
    public const string AdministratorRoleRevoked = "ADMIN_ROLE_REVOKED";
    public const string AdministratorLoginSucceeded = "ADMIN_LOGIN_SUCCEEDED";
    public const string AdministratorLoginFailed = "ADMIN_LOGIN_FAILED";
    public const string AdministratorLockedOut = "ADMIN_LOCKED_OUT";
    public const string AdministratorPasswordChanged = "ADMIN_PASSWORD_CHANGED";
    public const string AdministratorTwoFactorChanged = "ADMIN_TWO_FACTOR_CHANGED";
    public const string OutboxMessageReplayed = "OUTBOX_MESSAGE_REPLAYED";
}
