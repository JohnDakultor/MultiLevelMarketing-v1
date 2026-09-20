import type { ApiClient } from "@modular-mlm/api-client";
import type {
  AgentEarningsSummaryDto,
  AgentApplicationDto,
  AgentProductSalesPageDto,
  AgentLegSummaryDto,
  AgentProfileDto,
  AgentQualificationStatusDto,
  AgentReportDto,
  AttributedOrdersPageDto,
  AttributedOrderDetailsDto,
  BinaryTreeNodeDto,
  BinaryPairingRunDto,
  BinaryVolumeSummaryDto,
  CommissionHistoryItemDto,
  CurrentAgentContextDto,
  DirectRecruitDto,
  DownlineAgentDto,
  PlacementAncestorDto,
  PlacementChildDto,
  PayoutAccountDto,
  PayoutHistoryItemDto,
  PayoutDetailsDto,
  ProductReferralLinkDto,
  ReferralLinkDto,
  ReferralDashboardDto,
  WalletEntriesPageDto,
  WalletSummaryDto,
  ProductDto,
} from "@modular-mlm/contracts";

const org = (id: string) => `/api/organizations/${id}`;
const agent = (organizationId: string, agentId: string) =>
  `${org(organizationId)}/agents/${agentId}`;

export const agentApi = {
  application: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<AgentApplicationDto>(
      `${org(organizationId)}/agent/application`,
      { signal },
    ),
  context: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<CurrentAgentContextDto>(
      `${org(organizationId)}/agent/context`,
      { signal },
    ),
  profile: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<AgentProfileDto>(`${org(organizationId)}/agent/profile`, {
      signal,
    }),
  qualification: (
    api: ApiClient,
    organizationId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AgentQualificationStatusDto>(
      `${org(organizationId)}/agent/qualification`,
      { signal },
    ),
  setPreferredLeg: (
    api: ApiClient,
    organizationId: string,
    preferredLeg: number,
  ) =>
    api.request<void>(`${org(organizationId)}/agent/preferred-leg`, {
      method: "PUT",
      body: { preferredLeg },
    }),
  report: (
    api: ApiClient,
    organizationId: string,
    from: string,
    to: string,
    signal?: AbortSignal,
  ) =>
    api.request<AgentReportDto>(
      `${org(organizationId)}/reports/agent?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
      { signal },
    ),
  tree: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    depth: number,
    signal?: AbortSignal,
  ) =>
    api.request<BinaryTreeNodeDto[]>(
      `${agent(organizationId, agentId)}/network/tree?depth=${depth}`,
      { signal },
    ),
  downline: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    maxDepth: number,
    signal?: AbortSignal,
  ) =>
    api.request<DownlineAgentDto[]>(
      `${agent(organizationId, agentId)}/network/downline?maxDepth=${maxDepth}`,
      { signal },
    ),
  children: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<PlacementChildDto[]>(
      `${agent(organizationId, agentId)}/network/children`,
      { signal },
    ),
  ancestors: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<PlacementAncestorDto[]>(
      `${agent(organizationId, agentId)}/network/ancestors`,
      { signal },
    ),
  recruits: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<DirectRecruitDto[]>(
      `${agent(organizationId, agentId)}/network/recruits`,
      { signal },
    ),
  legSummary: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AgentLegSummaryDto>(
      `${agent(organizationId, agentId)}/network/leg-summary`,
      { signal },
    ),
  sales: (
    api: ApiClient,
    organizationId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<AttributedOrdersPageDto>(
      `${org(organizationId)}/agent/sales?page=${page}&pageSize=20`,
      { signal },
    ),
  saleDetails: (
    api: ApiClient,
    organizationId: string,
    orderId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AttributedOrderDetailsDto>(
      `${org(organizationId)}/agent/sales/${orderId}`,
      { signal },
    ),
  productSales: (
    api: ApiClient,
    organizationId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<AgentProductSalesPageDto>(
      `${org(organizationId)}/agent/sales/products?page=${page}&pageSize=20`,
      { signal },
    ),
  earnings: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AgentEarningsSummaryDto>(
      `${agent(organizationId, agentId)}/finance/earnings`,
      { signal },
    ),
  commissions: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<CommissionHistoryItemDto[]>(
      `${agent(organizationId, agentId)}/finance/commissions?page=${page}&pageSize=20`,
      { signal },
    ),
  commission: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    commissionId: string,
    signal?: AbortSignal,
  ) =>
    api.request<CommissionHistoryItemDto>(
      `${agent(organizationId, agentId)}/finance/commissions/${commissionId}`,
      { signal },
    ),
  binaryVolume: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<BinaryVolumeSummaryDto>(
      `${agent(organizationId, agentId)}/finance/binary-volume`,
      { signal },
    ),
  pairingHistory: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) => {
    const periodEnd = new Date();
    const periodStart = new Date(periodEnd);
    periodStart.setDate(periodStart.getDate() - 90);
    return api.request<BinaryPairingRunDto[]>(
      `${agent(organizationId, agentId)}/pairing?periodStart=${encodeURIComponent(periodStart.toISOString())}&periodEnd=${encodeURIComponent(periodEnd.toISOString())}`,
      { signal },
    );
  },
  wallet: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<WalletSummaryDto>(`${agent(organizationId, agentId)}/wallet`, {
      signal,
    }),
  walletEntries: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<WalletEntriesPageDto>(
      `${agent(organizationId, agentId)}/wallet/entries?page=${page}&pageSize=20`,
      { signal },
    ),
  payoutAccounts: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<PayoutAccountDto[]>(
      `${agent(organizationId, agentId)}/payouts/accounts`,
      { signal },
    ),
  payouts: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<PayoutHistoryItemDto[]>(
      `${agent(organizationId, agentId)}/payouts?page=${page}&pageSize=20`,
      { signal },
    ),
  payout: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    payoutId: string,
    signal?: AbortSignal,
  ) =>
    api.request<PayoutDetailsDto>(
      `${agent(organizationId, agentId)}/payouts/${payoutId}`,
      { signal },
    ),
  requestPayout: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    payoutAccountId: string,
    amount: number,
    currency: string,
  ) =>
    api.request<string>(`${agent(organizationId, agentId)}/finance/payouts`, {
      method: "POST",
      body: { payoutAccountId, amount, currency },
    }),
  cancelPayout: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    payoutId: string,
  ) =>
    api.request<void>(
      `${agent(organizationId, agentId)}/payouts/${payoutId}/cancel`,
      { method: "POST" },
    ),
  registerPayoutAccount: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    request: {
      method: string;
      accountName: string;
      accountNumber: string;
      bankCode: string;
      rail: string;
    },
  ) =>
    api.request<string>(`${agent(organizationId, agentId)}/payouts/accounts`, {
      method: "POST",
      body: request,
    }),
  submitPayoutAccount: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    accountId: string,
  ) =>
    api.request<void>(
      `${agent(organizationId, agentId)}/payouts/accounts/${accountId}/submit`,
      { method: "POST" },
    ),
  makeDefaultPayoutAccount: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    accountId: string,
  ) =>
    api.request<void>(
      `${agent(organizationId, agentId)}/payouts/accounts/${accountId}/default`,
      { method: "POST" },
    ),
  referral: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<ReferralLinkDto>(`${org(organizationId)}/agent/referral`, {
      signal,
    }),
  referralDashboard: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<ReferralDashboardDto>(
      `${org(organizationId)}/referrals/agents/${agentId}/dashboard`,
      { signal },
    ),
  regenerateReferralCode: (
    api: ApiClient,
    organizationId: string,
    agentId: string,
  ) =>
    api.request<string>(
      `${org(organizationId)}/referrals/agents/${agentId}/code`,
      { method: "POST" },
    ),
  productReferral: (
    api: ApiClient,
    organizationId: string,
    productId: string,
  ) =>
    api.request<ProductReferralLinkDto>(
      `${org(organizationId)}/agent/referral/product-links`,
      { method: "POST", body: { productId } },
    ),
  products: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<ProductDto[]>(
      `${org(organizationId)}/products?page=1&pageSize=100`,
      { anonymous: true, signal },
    ),
};
