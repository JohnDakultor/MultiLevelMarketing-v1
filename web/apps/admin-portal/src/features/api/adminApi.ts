import type { ApiClient } from "@modular-mlm/api-client";
import type {
  AdminAgentSummaryDto,
  AdminCategoryDto,
  AdminDashboardDto,
  AdminAgentDetailsDto,
  AdminOrderDetailsDto,
  AdminOrdersPageDto,
  AdminOrganizationSettingsDto,
  AdminProductDetailsDto,
  AdminProductDto,
  AdminReportDto,
  AdministratorDto,
  AdministratorInvitationDto,
  AgentApplicationSummaryDto,
  AuditLogDto,
  CommissionPlanDto,
  CreateOrganizationRequest,
  DeadLetterMessagesPageDto,
  InventoryPageDto,
  InventoryHistoryPageDto,
  OperationalHealthDto,
  PayoutHistoryItemDto,
  PayoutDetailsDto,
  ProductCommissionProfileDto,
  StoredObjectDto,
  UpdateBrandingRequest,
  WalletSettingsDto,
  AdminWalletsPageDto,
  AdminWalletEntriesPageDto,
  AdminCommissionLedgerPageDto,
  ReferralSettingsDto,
} from "@modular-mlm/contracts";

const org = (id: string) => `/api/organizations/${id}`;
const admin = (id: string) => `${org(id)}/admin`;

export const adminApi = {
  createOrganization: (api: ApiClient, body: CreateOrganizationRequest) =>
    api.request<string>("/api/organizations", { method: "POST", body }),
  uploadBrandingAsset: (
    api: ApiClient,
    organizationId: string,
    assetKind: "Logo" | "Favicon",
    file: File,
  ) => {
    const body = new FormData();
    body.append("file", file);
    return api.request<StoredObjectDto>(
      `${admin(organizationId)}/branding/assets/${assetKind}`,
      { method: "POST", body },
    );
  },
  dashboard: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AdminDashboardDto>(`${org(id)}/reports/admin/dashboard`, {
      signal,
    }),
  report: (
    api: ApiClient,
    id: string,
    from: string,
    to: string,
    signal?: AbortSignal,
  ) =>
    api.request<AdminReportDto>(
      `${org(id)}/reports/admin?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
      { signal },
    ),
  settings: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AdminOrganizationSettingsDto>(`${admin(id)}/settings`, {
      signal,
    }),
  walletSettings: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<WalletSettingsDto>(`${org(id)}/wallet-settings`, { signal }),
  updateProfile: (
    api: ApiClient,
    id: string,
    body: {
      name: string;
      currencyCode: string;
      timeZone: string;
      locale: string;
    },
  ) => api.request<void>(`${admin(id)}/profile`, { method: "PUT", body }),
  updateCommerce: (
    api: ApiClient,
    id: string,
    body: {
      allowGuestCheckout: boolean;
      requireShippingAddress: boolean;
      requireBillingAddress: boolean;
      inventoryReservationMinutes: number;
    },
  ) =>
    api.request<void>(`${admin(id)}/commerce-settings`, {
      method: "PUT",
      body,
    }),
  updateBranding: (api: ApiClient, id: string, body: UpdateBrandingRequest) =>
    api.request<void>(`${org(id)}/branding`, { method: "PUT", body }),
  publishBranding: (api: ApiClient, id: string) =>
    api.request<void>(`${admin(id)}/branding/publish`, { method: "POST" }),
  updateFeatures: (api: ApiClient, id: string, body: object) =>
    api.request<void>(`${org(id)}/feature-settings`, { method: "PUT", body }),
  updateWallet: (api: ApiClient, id: string, body: object) =>
    api.request<void>(`${org(id)}/wallet-settings`, { method: "PUT", body }),
  configureDomain: (
    api: ApiClient,
    id: string,
    hostName: string,
    makePrimary: boolean,
  ) =>
    api.request<string>(`${admin(id)}/domains`, {
      method: "POST",
      body: { hostName, makePrimary },
    }),
  removeDomain: (api: ApiClient, id: string, domainId: string) =>
    api.request<void>(`${admin(id)}/domains/${domainId}`, { method: "DELETE" }),
  products: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AdminProductDto[]>(
      `${admin(id)}/products?page=1&pageSize=100`,
      { signal },
    ),
  product: (
    api: ApiClient,
    id: string,
    productId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AdminProductDetailsDto>(`${admin(id)}/products/${productId}`, {
      signal,
    }),
  createVariant: (
    api: ApiClient,
    id: string,
    productId: string,
    body: object,
  ) =>
    api.request<string>(`${admin(id)}/products/${productId}/variants`, {
      method: "POST",
      body,
    }),
  updateProduct: (
    api: ApiClient,
    id: string,
    productId: string,
    body: { categoryId: string; name: string; description: string },
  ) =>
    api.request<void>(`${admin(id)}/products/${productId}`, {
      method: "PUT",
      body,
    }),
  updateVariant: (
    api: ApiClient,
    id: string,
    productId: string,
    variantId: string,
    body: object,
  ) =>
    api.request<void>(
      `${admin(id)}/products/${productId}/variants/${variantId}`,
      { method: "PUT", body },
    ),
  archiveVariant: (
    api: ApiClient,
    id: string,
    productId: string,
    variantId: string,
    reason: string,
    expectedVersion: number,
  ) =>
    api.request<string>(
      `${admin(id)}/products/${productId}/variants/${variantId}/archive`,
      { method: "POST", body: { reason, expectedVersion } },
    ),
  createCommissionProfile: (api: ApiClient, id: string, body: object) =>
    api.request<string>(`${org(id)}/products/commission-profiles`, {
      method: "POST",
      body,
    }),
  commissionProfiles: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<ProductCommissionProfileDto[]>(
      `${org(id)}/products/commission-profiles`,
      { signal },
    ),
  assignCommissionProfile: (
    api: ApiClient,
    id: string,
    productId: string,
    commissionProfileId: string | null,
  ) =>
    api.request<void>(`${org(id)}/products/${productId}/commission-profile`, {
      method: "PUT",
      body: { commissionProfileId },
    }),
  categories: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<{
      items: AdminCategoryDto[];
      page: number;
      pageSize: number;
      totalCount: number;
    }>(
      `${admin(id)}/categories?includeInactive=true&page=1&pageSize=100`,
      { signal },
    ),
  createCategory: (api: ApiClient, id: string, name: string, slug: string) =>
    api.request<string>(`${admin(id)}/categories`, {
      method: "POST",
      body: { name, slug },
    }),
  renameCategory: (api: ApiClient, id: string, categoryId: string, name: string) =>
    api.request<void>(`${admin(id)}/categories/${categoryId}`, {
      method: "PUT",
      body: { name },
    }),
  categoryAction: (
    api: ApiClient,
    id: string,
    categoryId: string,
    action: "archive" | "activate",
  ) => api.request<void>(`${admin(id)}/categories/${categoryId}/${action}`, { method: "POST" }),
  createProduct: (api: ApiClient, id: string, body: object) =>
    api.request<string>(`${org(id)}/products`, { method: "POST", body }),
  publishProduct: (api: ApiClient, id: string, productId: string) =>
    api.request<void>(`${admin(id)}/products/${productId}/publish`, {
      method: "POST",
    }),
  archiveProduct: (api: ApiClient, id: string, productId: string) =>
    api.request<void>(`${admin(id)}/products/${productId}/archive`, {
      method: "POST",
    }),
  inventory: (
    api: ApiClient,
    id: string,
    page: number,
    search: string,
    lowStock: boolean,
    signal?: AbortSignal,
  ) =>
    api.request<InventoryPageDto>(
      `${admin(id)}/inventory?page=${page}&pageSize=20&search=${encodeURIComponent(search)}&lowStockOnly=${lowStock}`,
      { signal },
    ),
  adjustInventory: (
    api: ApiClient,
    id: string,
    variantId: string,
    body: object,
    idempotencyKey: string,
  ) =>
    api.request<string>(`${admin(id)}/inventory/${variantId}/adjustments`, {
      method: "POST",
      headers: { "Idempotency-Key": idempotencyKey },
      body,
    }),
  inventoryHistory: (
    api: ApiClient,
    id: string,
    variantId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<InventoryHistoryPageDto>(
      `${admin(id)}/inventory/${variantId}/history?page=${page}&pageSize=20`,
      { signal },
    ),
  agents: (
    api: ApiClient,
    id: string,
    page: number,
    search: string,
    signal?: AbortSignal,
  ) =>
    api.request<{
      items: AdminAgentSummaryDto[];
      page: number;
      pageSize: number;
      totalCount: number;
      hasNextPage?: boolean;
      hasPreviousPage?: boolean;
    }>(
      `${admin(id)}/agents?page=${page}&pageSize=20&search=${encodeURIComponent(search)}`,
      { signal },
    ),
  placementCandidates: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<{
      items: AdminAgentSummaryDto[];
      page: number;
      pageSize: number;
      totalCount: number;
      hasNextPage?: boolean;
      hasPreviousPage?: boolean;
    }>(`${admin(id)}/agents?page=1&pageSize=100&search=`, { signal }),
  applications: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<{
      items: AgentApplicationSummaryDto[];
      page: number;
      pageSize: number;
      totalCount: number;
    }>(`${admin(id)}/agents/applications?page=1&pageSize=50`, { signal }),
  agentDetails: (
    api: ApiClient,
    id: string,
    agentId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AdminAgentDetailsDto>(`${admin(id)}/agents/${agentId}`, {
      signal,
    }),
  placeAgent: (
    api: ApiClient,
    id: string,
    agentId: string,
    parentAgentId: string,
    side: number,
  ) =>
    api.request<void>(`${org(id)}/agents/${agentId}/placement`, {
      method: "POST",
      body: { parentAgentId, side },
    }),
  moveAgentPlacement: (
    api: ApiClient,
    id: string,
    agentId: string,
    body: object,
  ) =>
    api.request<void>(
      `${admin(id)}/agents/${agentId}/move-uncommitted-placement`,
      {
        method: "POST",
        body,
      },
    ),
  processPairing: (api: ApiClient, id: string, agentId: string, body: object) =>
    api.request<string>(`${org(id)}/agents/${agentId}/pairing/runs`, {
      method: "POST",
      body,
    }),
  agentAction: (
    api: ApiClient,
    id: string,
    agentId: string,
    action: "approve" | "reject" | "activate" | "suspend" | "reactivate",
  ) =>
    api.request<void>(`${org(id)}/agents/${agentId}/${action}`, {
      method: "POST",
    }),
  plans: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<CommissionPlanDto[]>(`${admin(id)}/compensation/plans`, {
      signal,
    }),
  plan: (api: ApiClient, id: string, planId: string, signal?: AbortSignal) =>
    api.request<CommissionPlanDto>(
      `${admin(id)}/compensation/plans/${planId}`,
      { signal },
    ),
  updatePlan: (api: ApiClient, id: string, planId: string, body: object) =>
    api.request<void>(`${admin(id)}/compensation/plans/${planId}`, {
      method: "PUT",
      body,
    }),
  createPlan: (api: ApiClient, id: string, body: object) =>
    api.request<string>(`${admin(id)}/compensation/plans`, {
      method: "POST",
      body,
    }),
  planAction: (
    api: ApiClient,
    id: string,
    planId: string,
    action: "publish" | "retire",
    body?: object,
  ) =>
    api.request<void>(`${admin(id)}/compensation/plans/${planId}/${action}`, {
      method: "POST",
      body,
    }),
  payouts: (api: ApiClient, id: string, page: number, signal?: AbortSignal) =>
    api.request<PayoutHistoryItemDto[]>(
      `${admin(id)}/payouts?page=${page}&pageSize=20`,
      { signal },
    ),
  payoutDetails: (
    api: ApiClient,
    id: string,
    payoutId: string,
    signal?: AbortSignal,
  ) =>
    api.request<PayoutDetailsDto>(`${admin(id)}/payouts/${payoutId}`, {
      signal,
    }),
  payoutAccountAction: (
    api: ApiClient,
    id: string,
    accountId: string,
    action: "verify" | "reject",
  ) =>
    api.request<void>(`${admin(id)}/payouts/accounts/${accountId}/${action}`, {
      method: "POST",
    }),
  orders: (
    api: ApiClient,
    id: string,
    parameters: URLSearchParams,
    signal?: AbortSignal,
  ) =>
    api.request<AdminOrdersPageDto>(`${admin(id)}/orders?${parameters}`, {
      signal,
    }),
  orderDetails: (
    api: ApiClient,
    id: string,
    orderId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AdminOrderDetailsDto>(`${admin(id)}/orders/${orderId}`, {
      signal,
    }),
  startOrderProcessing: (api: ApiClient, id: string, orderId: string) =>
    api.request<void>(`${admin(id)}/orders/${orderId}/processing`, {
      method: "POST",
    }),
  shipOrder: (
    api: ApiClient,
    id: string,
    orderId: string,
    carrier: string | null,
    trackingNumber: string | null,
  ) =>
    api.request<void>(`${admin(id)}/orders/${orderId}/ship`, {
      method: "POST",
      body: { carrier, trackingNumber },
    }),
  deliverOrder: (api: ApiClient, id: string, orderId: string) =>
    api.request<void>(`${admin(id)}/orders/${orderId}/deliver`, {
      method: "POST",
    }),
  cancelOrder: (api: ApiClient, id: string, orderId: string, reason: string) =>
    api.request<void>(`${admin(id)}/orders/${orderId}/cancel`, {
      method: "POST",
      body: { reason },
    }),
  requestPaymentRefund: (
    api: ApiClient,
    id: string,
    orderId: string,
    amount: number,
    reason: string,
  ) =>
    api.request<string>(`${org(id)}/orders/${orderId}/refunds`, {
      method: "POST",
      body: { amount, reason },
    }),
  reconcilePayment: (api: ApiClient, id: string, paymentId: string) =>
    api.request<boolean>(`${org(id)}/orders/payments/${paymentId}/reconcile`, {
      method: "POST",
    }),
  requestItemRefund: (
    api: ApiClient,
    id: string,
    orderId: string,
    orderItemId: string,
    quantity: number,
    reason: string,
  ) =>
    api.request<string>(
      `${org(id)}/orders/${orderId}/refunds/items/${orderItemId}`,
      { method: "POST", body: { quantity, reason } },
    ),
  reconcileItemRefund: (
    api: ApiClient,
    id: string,
    orderId: string,
    refundId: string,
  ) =>
    api.request<boolean>(
      `${org(id)}/orders/${orderId}/refunds/${refundId}/reconcile`,
      { method: "POST" },
    ),
  updateNetwork: (api: ApiClient, id: string, body: object) =>
    api.request<void>(`${org(id)}/network-settings`, { method: "PUT", body }),
  autoPlaceAgent: (api: ApiClient, id: string, agentId: string) =>
    api.request<unknown>(`${org(id)}/agents/${agentId}/auto-placement`, {
      method: "POST",
      body: { strategy: null, preferredSide: null },
    }),
  adjustWallet: (api: ApiClient, id: string, agentId: string, body: object) =>
    api.request<string>(`${org(id)}/agents/${agentId}/wallet/adjustments`, {
      method: "POST",
      body,
    }),
  payoutAction: (
    api: ApiClient,
    id: string,
    payoutId: string,
    action: "approve" | "reject" | "process" | "reconcile",
  ) =>
    api.request<unknown>(`${admin(id)}/payouts/${payoutId}/${action}`, {
      method: "POST",
    }),
  administrators: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AdministratorDto[]>(`${org(id)}/administrators`, { signal }),
  invitations: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AdministratorInvitationDto[]>(
      `${org(id)}/administrators/invitations`,
      { signal },
    ),
  invite: (api: ApiClient, id: string, email: string) =>
    api.request<string>(`${org(id)}/administrators/invitations`, {
      method: "POST",
      body: { email },
    }),
  revokeInvitation: (
    api: ApiClient,
    id: string,
    invitationId: string,
    reason: string,
  ) =>
    api.request<void>(
      `${org(id)}/administrators/invitations/${invitationId}/revoke`,
      { method: "POST", body: { reason } },
    ),
  revokeAdministrator: (
    api: ApiClient,
    id: string,
    userId: string,
    reason: string,
  ) =>
    api.request<void>(`${org(id)}/administrators/${userId}/revoke-role`, {
      method: "POST",
      body: { reason },
    }),
  adminWallets: (
    api: ApiClient,
    id: string,
    page: number,
    search: string,
    signal?: AbortSignal,
  ) =>
    api.request<AdminWalletsPageDto>(
      `${admin(id)}/wallets?page=${page}&pageSize=20&search=${encodeURIComponent(search)}&negativeOnly=false`,
      { signal },
    ),
  adminWalletEntries: (
    api: ApiClient,
    id: string,
    agentId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<AdminWalletEntriesPageDto>(
      `${admin(id)}/wallets/${agentId}/entries?page=${page}&pageSize=20`,
      { signal },
    ),
  commissionLedger: (
    api: ApiClient,
    id: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<AdminCommissionLedgerPageDto>(
      `${admin(id)}/commissions?page=${page}&pageSize=20&includeReversals=true`,
      { signal },
    ),
  referralSettings: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<ReferralSettingsDto>(`${org(id)}/referral-settings`, { signal }),
  updateReferralSettings: (
    api: ApiClient,
    id: string,
    body: ReferralSettingsDto,
  ) => api.request<void>(`${org(id)}/referral-settings`, { method: "PUT", body }),
  audit: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<AuditLogDto[]>(`${admin(id)}/audit-trail?page=1&pageSize=50`, {
      signal,
    }),
  health: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<OperationalHealthDto>(`${admin(id)}/operational-health`, {
      signal,
    }),
  deadLetters: (api: ApiClient, id: string, signal?: AbortSignal) =>
    api.request<DeadLetterMessagesPageDto>(
      `${admin(id)}/messaging/dead-letters?page=1&pageSize=20`,
      { signal },
    ),
  replayDeadLetter: (
    api: ApiClient,
    id: string,
    messageId: string,
    expectedAttempts: number,
    reason: string,
  ) =>
    api.request<unknown>(
      `${admin(id)}/messaging/dead-letters/${messageId}/replay`,
      { method: "POST", body: { expectedAttempts, reason } },
    ),
};
