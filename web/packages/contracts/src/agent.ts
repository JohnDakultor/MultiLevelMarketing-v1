import type { Guid, IsoDateTime, Page } from "./common";

export interface CurrentAgentContextDto {
  agentId: Guid;
  agentCode: string;
  status: number;
  isPlaced: boolean;
  canChoosePreferredLeg: boolean;
  canShareReferralLinks: boolean;
  canRequestPayout: boolean;
  qualificationState: string;
}
export interface AgentApplicationDto {
  agentId: Guid;
  agentCode: string;
  status: number;
  sponsorAgentId: Guid | null;
  sponsorAgentCode: string | null;
  joinedAt: IsoDateTime;
  activatedAt: IsoDateTime | null;
  qualificationState: string;
  isPlacementPending: boolean;
}
export interface AgentProfileDto {
  agentId: Guid;
  agentCode: string;
  referralCode: string;
  displayName: string;
  email: string;
  status: number;
  sponsorAgentId: Guid | null;
  sponsorAgentCode: string | null;
  placementParentAgentId: Guid | null;
  placementParentAgentCode: string | null;
  placementSide: number | null;
  preferredLeg: number | null;
  joinedAt: IsoDateTime;
  activatedAt: IsoDateTime | null;
  qualificationState: string;
}
export interface AgentQualificationFailureDto {
  code: string;
  message: string;
}
export interface AgentQualificationStatusDto {
  isQualified: boolean;
  state: string;
  failures: AgentQualificationFailureDto[];
  personalSalesAmount: number;
  personalBusinessVolume: number;
  activeDirectRecruitCount: number;
  hasActiveLeftLeg: boolean;
  hasActiveRightLeg: boolean;
  commissionPlanId: Guid | null;
  commissionPlanVersion: number | null;
  effectiveAt: IsoDateTime | null;
  evaluatedAt: IsoDateTime;
}
export interface BinaryTreeNodeDto {
  agentId: Guid;
  agentCode: string;
  referralCode: string;
  status: number;
  parentAgentId: Guid | null;
  side: number | null;
  depth: number;
}
export interface DownlineAgentDto {
  agentId: Guid;
  agentCode: string;
  status: number;
  placementParentAgentId: Guid | null;
  placementSide: number | null;
  depth: number;
  firstLeg: number;
  joinedAt: IsoDateTime;
}
export interface PlacementChildDto {
  agentId: Guid;
  agentCode: string;
  status: number;
  side: number;
  joinedAt: IsoDateTime;
}
export interface PlacementAncestorDto {
  agentId: Guid;
  agentCode: string;
  status: number;
  depth: number;
  descendantLeg: number;
}
export interface DirectRecruitDto {
  agentId: Guid;
  agentCode: string;
  referralCode: string;
  status: number;
  placementParentAgentId: Guid | null;
  placementSide: number | null;
  joinedAt: IsoDateTime;
}
export interface AgentLegSummaryDto {
  agentId: Guid;
  leftAgentCount: number;
  rightAgentCount: number;
  leftAvailableVolume: number;
  rightAvailableVolume: number;
  leftLifetimeVolume: number;
  rightLifetimeVolume: number;
}
export interface AgentEarningsSummaryDto {
  agentId: Guid;
  pendingAmount: number;
  availableAmount: number;
  paidAmount: number;
  directSalesLifetime: number;
  binaryPairingLifetime: number;
}
export interface BinaryVolumeSummaryDto {
  agentId: Guid;
  leftAvailable: number;
  rightAvailable: number;
  leftLifetime: number;
  rightLifetime: number;
}
export interface BinaryVolumeLedgerItemDto {
  id: Guid;
  ownerAgentId: Guid;
  sourceAgentId: Guid | null;
  sourceOrderItemId: Guid | null;
  pairingRunId: Guid | null;
  side: number;
  volume: number;
  entryType: number;
  effectiveAt: IsoDateTime;
  reversalOfEntryId: Guid | null;
  sourceOrderItemRefundId: Guid | null;
}
export interface BinaryVolumeLedgerPageDto extends Page<BinaryVolumeLedgerItemDto> {
  organizationId: Guid;
  agentId: Guid;
}
export interface BinaryPairingRunDto {
  organizationId: Guid;
  agentId: Guid;
  commissionPlanId: Guid;
  commissionPlanVersion: number;
  periodStart: IsoDateTime;
  periodEnd: IsoDateTime;
  qualificationPassed: boolean;
  qualificationFailureReason: string | null;
  leftBefore: number;
  rightBefore: number;
  matchedVolume: number;
  leftConsumed: number;
  rightConsumed: number;
  leftAfter: number;
  rightAfter: number;
  grossCommission: number;
  cappedAmount: number;
  netCommission: number;
  capApplied: boolean;
  status: number;
  processedAt: IsoDateTime | null;
}
export interface CommissionHistoryItemDto {
  id: Guid;
  sourceOrderId: Guid | null;
  sourceOrderItemId: Guid | null;
  pairingRunId: Guid | null;
  type: number;
  baseAmount: number;
  rate: number | null;
  amount: number;
  status: number;
  created: IsoDateTime;
}
export interface WalletSummaryDto {
  walletId: Guid;
  currency: string;
  pending: number;
  available: number;
  held: number;
  paidLifetime: number;
  recoverableNegative: number;
  net: number;
}
export interface WalletEntryDto {
  id: Guid;
  type: number;
  amount: number;
  sourceType: string;
  sourceId: Guid;
  availableAt: IsoDateTime | null;
  reversalOfEntryId: Guid | null;
  releasedFromEntryId: Guid | null;
  createdAt: IsoDateTime;
}
export type WalletEntriesPageDto = Page<WalletEntryDto>;
export interface PayoutHistoryItemDto {
  id: Guid;
  agentId: Guid;
  amount: number;
  currency: string;
  status: number;
  requestedAt: IsoDateTime;
  processedAt: IsoDateTime | null;
  providerReference: string | null;
}
export interface PayoutDetailsDto extends PayoutHistoryItemDto {
  payoutAccountId: Guid;
  approvedAt: IsoDateTime | null;
  providerBatchId: string | null;
  providerTransferId: string | null;
  failureCode: string | null;
  failureMessage: string | null;
}
export interface PayoutAccountDto {
  id: Guid;
  method: string;
  maskedAccountData: string;
  bankCode: string;
  rail: string;
  verificationStatus: number;
  isDefault: boolean;
}
export interface ReferralLinkDto {
  referralCode: string;
  productId: Guid | null;
  relativeUrl: string;
}
export interface ProductReferralLinkDto {
  productId: Guid;
  productSlug: string;
  referralCode: string;
  canonicalUrl: string;
  qrPayload: string;
}
export interface AttributedOrderSummaryDto {
  orderId: Guid;
  orderNumber: string;
  createdAt: IsoDateTime;
  status: number;
  paymentStatus: number;
  currency: string;
  grandTotal: number;
  commissionableAmount: number;
  businessVolume: number;
  itemCount: number;
  productNames: string[];
  maskedCustomerName: string;
  commissionAmount: number;
}
export type AttributedOrdersPageDto = Page<AttributedOrderSummaryDto>;
export interface AttributedOrderItemDto {
  orderItemId: Guid;
  productId: Guid;
  productVariantId: Guid;
  productName: string;
  sku: string;
  quantity: number;
  fulfillmentStatus: number;
  commissionableAmount: number;
  businessVolume: number;
}
export interface AgentCommissionSummaryDto {
  commissionId: Guid;
  type: number;
  status: number;
  amount: number;
  sourceOrderItemId: Guid | null;
  reversalOfCommissionId: Guid | null;
}
export interface AttributedOrderDetailsDto {
  orderId: Guid;
  orderNumber: string;
  createdAt: IsoDateTime;
  paidAt: IsoDateTime | null;
  deliveredAt: IsoDateTime | null;
  status: number;
  paymentStatus: number;
  currency: string;
  grandTotal: number;
  maskedCustomerName: string;
  items: AttributedOrderItemDto[];
  commissions: AgentCommissionSummaryDto[];
}
export interface AgentProductSalesSummaryDto {
  productId: Guid;
  productVariantId: Guid;
  productName: string;
  sku: string;
  quantitySold: number;
  currency: string;
  grossAttributedSales: number;
  commissionableSales: number;
  businessVolume: number;
  commissionAmount: number;
}
export type AgentProductSalesPageDto = Page<AgentProductSalesSummaryDto>;
export interface ReferralDashboardDto {
  agentId: Guid;
  referralCode: string;
  storefrontUrl: string;
  directRecruitCount: number;
  attributedOrderCount: number;
  attributedSales: number;
  pendingCommission: number;
  availableCommission: number;
}
