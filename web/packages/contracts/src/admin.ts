import type { Guid, IsoDateTime, Page } from "./common";
import type { PayoutHistoryItemDto } from "./agent";
import type { AddressDto } from "./commerce";

export interface AdminProductDto {
  id: Guid;
  categoryId: Guid;
  categoryName: string;
  name: string;
  slug: string;
  status: number;
  price: number;
  businessVolume: number;
  stockQuantity: number;
}
export interface AdminProductVariantDto {
  id: Guid;
  sku: string;
  status: number;
  price: number;
  businessVolume: number;
  weight: number | null;
  attributesJson: string;
  stockKeepingEnabled: boolean;
  onHand: number;
  reserved: number;
  available: number | null;
  version: number;
}
export interface AdminProductDetailsDto {
  id: Guid;
  name: string;
  slug: string;
  description: string;
  status: number;
  categoryId: Guid;
  categoryName: string;
  brand: string | null;
  defaultImageUrl: string | null;
  commissionProfileId: Guid | null;
  commissionProfileName: string | null;
  created: IsoDateTime;
  lastModified: IsoDateTime;
  variants: AdminProductVariantDto[];
}
export interface AdminCategoryDto {
  id: Guid;
  name: string;
  slug: string;
  isActive: boolean;
  productCount: number;
  created: IsoDateTime;
  lastModified: IsoDateTime;
}
export interface InventoryItemDto {
  productId: Guid;
  productName: string;
  productStatus: number;
  productVariantId: Guid;
  sku: string;
  variantStatus: number;
  onHand: number;
  reserved: number;
  available: number | null;
  version: number;
}
export interface InventoryHistoryItemDto {
  id: Guid;
  adjustmentType: number;
  quantityDelta: number;
  balanceBefore: number;
  balanceAfter: number;
  reason: string;
  actorUserId: Guid;
  occurredAt: IsoDateTime;
}
export interface InventoryHistoryPageDto {
  productVariantId: Guid;
  items: InventoryHistoryItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
export interface ProductCommissionProfileDto {
  id: Guid;
  name: string;
  directSalesEligible: boolean;
  directSalesRateOverride: number | null;
  binaryVolumeEligible: boolean;
  binaryVolumeOverride: number | null;
  effectiveFrom: IsoDateTime;
  effectiveTo: IsoDateTime | null;
}
export interface AdminAgentSummaryDto {
  agentId: Guid;
  agentCode: string;
  displayName: string;
  email: string;
  status: number;
  sponsorAgentCode: string | null;
  sponsorAgentId: Guid | null;
  isPlaced: boolean;
  placementParentAgentId: Guid | null;
  placementSide: number | null;
  joinedAt: IsoDateTime;
  activatedAt: IsoDateTime | null;
  qualificationState: string;
}
export interface AgentApplicationSummaryDto {
  agentId: Guid;
  displayName: string;
  email: string;
  agentCode: string;
  status: number;
  sponsorAgentId: Guid | null;
  sponsorAgentCode: string | null;
  joinedAt: IsoDateTime;
  isPlaced: boolean;
}
export interface AgentPlacementStatusDto {
  isPlaced: boolean;
  parentAgentId: Guid | null;
  parentAgentCode: string | null;
  side: number | null;
  preferredLeg: number | null;
  directChildAgentIds: Guid[];
  descendantCount: number;
  hasFinancialActivity: boolean;
  isMoveEligible: boolean;
  moveEligibilityReasonCode: string;
  moveEligibilityReason: string;
}
export interface AdminAgentDetailsDto {
  agentId: Guid;
  agentCode: string;
  referralCode: string;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  status: number;
  sponsorAgentId: Guid | null;
  sponsorAgentCode: string | null;
  joinedAt: IsoDateTime;
  activatedAt: IsoDateTime | null;
  qualificationState: string;
  placement: AgentPlacementStatusDto;
  canApprove: boolean;
  canActivate: boolean;
  canSuspend: boolean;
  canReactivate: boolean;
}
export interface CommissionPlanDto {
  id: Guid;
  name: string;
  version: number;
  status: number;
  effectiveFrom: IsoDateTime;
  effectiveTo: IsoDateTime | null;
  directSalesRate: number;
  binaryPairingEnabled: boolean;
  binaryPairingRate: number | null;
  processingFrequency: number;
  pairingCalculationType: number;
  pairUnitBv: number | null;
  fixedPairAmount: number | null;
  carryForwardEnabled: boolean;
  qualificationRulesJson: string;
  capRulesJson: string;
  configurationVersion: number;
}

export interface AdminWalletSummaryDto {
  walletId: Guid;
  agentId: Guid;
  agentCode: string;
  displayName: string | null;
  currency: string;
  status: number;
  pending: number;
  available: number;
  held: number;
  paidLifetime: number;
  recoverableNegative: number;
  net: number;
}
export type AdminWalletsPageDto = Page<AdminWalletSummaryDto>;
export interface AdminWalletEntryDto {
  id: Guid;
  walletId: Guid;
  type: number;
  amount: number;
  sourceType: string;
  sourceId: Guid;
  availableAt: IsoDateTime | null;
  reversalOfEntryId: Guid | null;
  releasedFromEntryId: Guid | null;
  createdAt: IsoDateTime;
}
export interface AdminWalletEntriesPageDto extends Page<AdminWalletEntryDto> {
  organizationId: Guid;
  agentId: Guid;
  walletId: Guid;
  agentCode: string;
  currency: string;
}
export interface AdminCommissionLedgerItemDto {
  id: Guid;
  beneficiaryAgentId: Guid;
  agentCode: string;
  sourceOrderId: Guid | null;
  sourceOrderItemId: Guid | null;
  sourceAgentId: Guid | null;
  pairingRunId: Guid | null;
  commissionPlanVersionId: Guid;
  ruleId: string;
  type: number;
  baseAmount: number;
  rate: number | null;
  amount: number;
  status: number;
  availableAt: IsoDateTime | null;
  reversalOfCommissionId: Guid | null;
  sourceOrderItemRefundId: Guid | null;
  createdAt: IsoDateTime;
}
export interface AdminCommissionLedgerTotalsDto {
  netAmount: number;
  pendingAmount: number;
  availableAmount: number;
  heldAmount: number;
  paidAmount: number;
  reversalAmount: number;
}
export interface AdminCommissionLedgerPageDto extends Page<AdminCommissionLedgerItemDto> {
  organizationId: Guid;
  currency: string;
  totals: AdminCommissionLedgerTotalsDto;
}
export interface AdministratorDto {
  id: string;
  organizationId: Guid;
  displayName: string;
  email: string;
  emailConfirmed: boolean;
  role: string;
}
export interface AdministratorInvitationDto {
  id: Guid;
  organizationId: Guid;
  invitedByUserId: Guid;
  email: string;
  invitedAt: IsoDateTime;
  expiresAt: IsoDateTime;
  status: number;
  acceptedByUserId: Guid | null;
  acceptedAt: IsoDateTime | null;
  revokedByUserId: Guid | null;
  revokedAt: IsoDateTime | null;
  revocationReason: string | null;
}
export interface AuditLogDto {
  id: Guid;
  organizationId: Guid;
  actorUserId: Guid;
  action: string;
  entityType: string;
  entityId: Guid;
  beforeJson: string | null;
  afterJson: string | null;
  reason: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  traceId: string | null;
  createdAt: IsoDateTime;
}
export interface HealthCheckDto {
  name: string;
  status: string;
  description?: string;
  durationMilliseconds?: number;
}
export interface OperationalHealthDto {
  organizationId: Guid;
  observedAt: IsoDateTime;
  pendingOutboxMessages: number;
  oldestPendingOutboxAgeSeconds: number | null;
  failedWebhookAttempts: number;
  paymentsAwaitingReconciliation: number;
  payoutsAwaitingProviderCompletion: number;
  compensationBacklog: number;
  recentPairingFailures: number;
  latestPairingDurationMilliseconds: number | null;
}
export interface DeadLetterMessageDto {
  messageId: Guid;
  messageType: string;
  attempts: number;
  lastErrorSummary: string | null;
  occurredAt: IsoDateTime;
  deadLetteredAt: IsoDateTime;
  nextAttemptAt: IsoDateTime;
  correlationId: string | null;
}
export type DeadLetterMessagesPageDto = Page<DeadLetterMessageDto>;
export type AdminProductsPageDto = Page<AdminProductDto>;
export type AdminAgentsPageDto = Page<AdminAgentSummaryDto>;
export type InventoryPageDto = Page<InventoryItemDto>;
export type AdministratorPayoutsDto = PayoutHistoryItemDto[];

export interface AdminOrderSummaryDto {
  id: Guid;
  orderNumber: string;
  customerId: Guid;
  customerDisplayName: string;
  status: number;
  paymentStatus: number;
  currency: string;
  grandTotal: number;
  itemCount: number;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  deliveredAt: IsoDateTime | null;
}
export type AdminOrdersPageDto = Page<AdminOrderSummaryDto>;
export interface RefundHistoryItemDto {
  id: Guid;
  orderId: Guid;
  orderItemId: Guid;
  quantity: number;
  amount: number;
  currency: string;
  reason: string;
  reversalStatus: number;
  providerStatus: number;
  providerRefundId: string | null;
  requestedAt: IsoDateTime;
  providerCompletedAt: IsoDateTime | null;
  reversalCompletedAt: IsoDateTime | null;
  failureSummary: string | null;
}
export interface AdminPaymentRefundDto {
  id: Guid;
  amount: number;
  currency: string;
  reason: string;
  providerRefundId: string | null;
  status: number;
  requestedAt: IsoDateTime;
  completedAt: IsoDateTime | null;
  failureMessage: string | null;
}
export interface AdminOrderPaymentDto {
  id: Guid;
  provider: string;
  providerCheckoutSessionId: string | null;
  providerPaymentId: string | null;
  amount: number;
  refundedAmount: number;
  currency: string;
  status: number;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  failureCode: string | null;
  failureMessage: string | null;
  refunds: AdminPaymentRefundDto[];
}
export interface AdminOrderItemDto {
  id: Guid;
  productId: Guid;
  productVariantId: Guid;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  businessVolume: number;
  fulfillmentStatus: number;
}
export interface AdminOrderDetailsDto {
  id: Guid;
  orderNumber: string;
  customerId: Guid;
  customerDisplayName: string;
  status: number;
  paymentStatus: number;
  currency: string;
  subtotal: number;
  discountTotal: number;
  shippingTotal: number;
  taxTotal: number;
  grandTotal: number;
  shippingAddress: AddressDto;
  billingAddress: AddressDto;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  deliveredAt: IsoDateTime | null;
  items: AdminOrderItemDto[];
  payments: AdminOrderPaymentDto[];
  itemRefunds: RefundHistoryItemDto[];
}
