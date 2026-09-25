import type {
  AdminDashboardDto,
  CartDto,
  CurrentUserDto,
  OrderDetailsDto,
  ProblemDetails,
  PublicOrganizationConfigDto,
  WalletSummaryDto,
} from "@modular-mlm/contracts";

export function currentUserFixture(
  overrides: Partial<CurrentUserDto> = {},
): CurrentUserDto {
  return {
    userId: "00000000-0000-0000-0000-000000000001",
    email: "user@example.test",
    displayName: "Test User",
    roles: [],
    organizationId: null,
    customerId: null,
    agentId: null,
    emailVerified: true,
    mfaEnabled: false,
    ...overrides,
  };
}

export function organizationFixture(
  overrides: Partial<PublicOrganizationConfigDto> = {},
): PublicOrganizationConfigDto {
  return {
    id: "00000000-0000-0000-0000-000000000010",
    name: "Test Organization",
    slug: "test-organization",
    currencyCode: "PHP",
    locale: "en-PH",
    storeTitle: "Test Store",
    primaryColor: "#173f35",
    secondaryColor: "#ffffff",
    accentColor: "#d79543",
    logoUrl: null,
    agentProgramEnabled: true,
    binaryNetworkEnabled: true,
    walletEnabled: true,
    payoutEnabled: true,
    ...overrides,
  };
}

export function problemDetailsFixture(
  overrides: Partial<ProblemDetails> = {},
): ProblemDetails {
  return {
    status: 400,
    title: "Validation failed",
    errors: {},
    ...overrides,
  };
}

export function cartFixture(overrides: Partial<CartDto> = {}): CartDto {
  return {
    id: null,
    organizationId: "00000000-0000-0000-0000-000000000010",
    customerId: null,
    hasAnonymousSession: false,
    attributedAgentId: null,
    referralCode: null,
    items: [],
    currency: "PHP",
    subtotal: 0,
    itemCount: 0,
    ...overrides,
  };
}

export function orderDetailsFixture(
  overrides: Partial<OrderDetailsDto> = {},
): OrderDetailsDto {
  const address = {
    recipientName: "Test Customer",
    phoneNumber: "09170000000",
    addressLine1: "1 Test Street",
    addressLine2: null,
    barangay: null,
    cityOrMunicipality: "Manila",
    province: "Metro Manila",
    postalCode: "1000",
    countryCode: "PH",
  };
  return {
    id: "00000000-0000-0000-0000-000000000020",
    orderNumber: "ORD-0001",
    status: 0,
    paymentStatus: 0,
    currency: "PHP",
    subtotal: 100,
    discountTotal: 0,
    shippingTotal: 0,
    taxTotal: 0,
    grandTotal: 100,
    shippingAddress: address,
    billingAddress: address,
    createdAt: "2026-09-19T00:00:00Z",
    paidAt: null,
    deliveredAt: null,
    canRequestCancellation: true,
    cancellationFailureReason: null,
    items: [],
    ...overrides,
  };
}

export function walletSummaryFixture(
  overrides: Partial<WalletSummaryDto> = {},
): WalletSummaryDto {
  return {
    walletId: "00000000-0000-0000-0000-000000000030",
    currency: "PHP",
    pending: 0,
    available: 0,
    held: 0,
    paidLifetime: 0,
    recoverableNegative: 0,
    net: 0,
    ...overrides,
  };
}

export function adminDashboardFixture(
  overrides: Partial<AdminDashboardDto> = {},
): AdminDashboardDto {
  return {
    organizationId: "00000000-0000-0000-0000-000000000010",
    observedAt: "2026-09-19T00:00:00Z",
    currency: "PHP",
    salesToday: 0,
    salesLast30Days: 0,
    ordersToday: 0,
    activeAgents: 0,
    commissionLiability: 0,
    walletLiability: 0,
    tasks: [],
    alerts: [],
    ...overrides,
  };
}
