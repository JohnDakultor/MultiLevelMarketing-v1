import type { ApiClient } from "@modular-mlm/api-client";
import type {
  AgentStorefrontDto,
  AgentApplicationDto,
  CartDto,
  CategoryDto,
  CheckoutAddressInput,
  CustomerAddressDto,
  CustomerAddressRequest,
  CustomerProfileDto,
  OrderDetailsDto,
  OrderSummariesPageDto,
  PaymentSessionDto,
  ProductDetailDto,
  ProductDto,
  IdentityInfoDto,
  TwoFactorRequest,
  TwoFactorResponse,
  UpdateIdentityInfoRequest,
} from "@modular-mlm/contracts";

const tenant = (organizationId: string) =>
  `/api/organizations/${organizationId}`;

export const storefrontApi = {
  identityInfo: (api: ApiClient, signal?: AbortSignal) =>
    api.request<IdentityInfoDto>("/api/Users/manage/info", { signal }),
  updateIdentityInfo: (api: ApiClient, body: UpdateIdentityInfoRequest) =>
    api.request<IdentityInfoDto>("/api/Users/manage/info", {
      method: "POST",
      body,
    }),
  updateTwoFactor: (api: ApiClient, body: TwoFactorRequest) =>
    api.request<TwoFactorResponse>("/api/Users/manage/2fa", {
      method: "POST",
      body,
    }),
  categories: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<CategoryDto[]>(`${tenant(organizationId)}/categories`, {
      anonymous: true,
      signal,
    }),
  products: (
    api: ApiClient,
    organizationId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<ProductDto[]>(
      `${tenant(organizationId)}/products?page=${page}&pageSize=24`,
      { anonymous: true, signal },
    ),
  product: (
    api: ApiClient,
    organizationId: string,
    slug: string,
    signal?: AbortSignal,
  ) =>
    api.request<ProductDetailDto>(
      `${tenant(organizationId)}/products/${encodeURIComponent(slug)}`,
      { anonymous: true, signal },
    ),
  referralStore: (
    api: ApiClient,
    organizationId: string,
    code: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<AgentStorefrontDto>(
      `${tenant(organizationId)}/referrals/store/${encodeURIComponent(code)}?pageNumber=${page}&pageSize=24`,
      { anonymous: true, signal },
    ),
  resolveReferral: (
    api: ApiClient,
    organizationId: string,
    code: string,
    signal?: AbortSignal,
  ) =>
    api.request<void>(
      `${tenant(organizationId)}/referrals/resolve/${encodeURIComponent(code)}`,
      { anonymous: true, signal },
    ),
  agentApplication: (
    api: ApiClient,
    organizationId: string,
    signal?: AbortSignal,
  ) =>
    api.request<AgentApplicationDto>(
      `${tenant(organizationId)}/agent/application`,
      { signal },
    ),
  applyAsAgent: (api: ApiClient, organizationId: string) =>
    api.request<string>(`${tenant(organizationId)}/agents/applications`, {
      method: "POST",
      body: { sponsorAgentId: null },
    }),
  cart: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<CartDto>(`${tenant(organizationId)}/cart`, {
      anonymous: true,
      signal,
    }),
  addCartItem: (
    api: ApiClient,
    organizationId: string,
    productVariantId: string,
    quantity: number,
  ) =>
    api.request<string>(`${tenant(organizationId)}/cart/items`, {
      method: "POST",
      anonymous: true,
      optionalAntiforgery: true,
      body: { productVariantId, quantity },
    }),
  updateCartItem: (
    api: ApiClient,
    organizationId: string,
    cartItemId: string,
    quantity: number,
  ) =>
    api.request<CartDto>(`${tenant(organizationId)}/cart/items/${cartItemId}`, {
      method: "PUT",
      anonymous: true,
      optionalAntiforgery: true,
      body: { quantity },
    }),
  removeCartItem: (
    api: ApiClient,
    organizationId: string,
    cartItemId: string,
  ) =>
    api.request<CartDto>(`${tenant(organizationId)}/cart/items/${cartItemId}`, {
      method: "DELETE",
      anonymous: true,
      optionalAntiforgery: true,
    }),
  applyReferral: (
    api: ApiClient,
    organizationId: string,
    referralCode: string,
  ) =>
    api.request<CartDto>(`${tenant(organizationId)}/cart/referral`, {
      method: "PUT",
      anonymous: true,
      optionalAntiforgery: true,
      body: { referralCode },
    }),
  profile: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<CustomerProfileDto>(`${tenant(organizationId)}/me/profile`, {
      signal,
    }),
  updateProfile: (
    api: ApiClient,
    organizationId: string,
    displayName: string,
  ) =>
    api.request<CustomerProfileDto>(`${tenant(organizationId)}/me/profile`, {
      method: "PUT",
      body: { displayName },
    }),
  addresses: (api: ApiClient, organizationId: string, signal?: AbortSignal) =>
    api.request<CustomerAddressDto[]>(
      `${tenant(organizationId)}/me/addresses`,
      { signal },
    ),
  addAddress: (
    api: ApiClient,
    organizationId: string,
    request: CustomerAddressRequest,
  ) =>
    api.request<string>(`${tenant(organizationId)}/me/addresses`, {
      method: "POST",
      body: request,
    }),
  updateAddress: (
    api: ApiClient,
    organizationId: string,
    addressId: string,
    request: CustomerAddressRequest,
  ) =>
    api.request<void>(`${tenant(organizationId)}/me/addresses/${addressId}`, {
      method: "PUT",
      body: request,
    }),
  removeAddress: (api: ApiClient, organizationId: string, addressId: string) =>
    api.request<void>(`${tenant(organizationId)}/me/addresses/${addressId}`, {
      method: "DELETE",
    }),
  checkout: (
    api: ApiClient,
    organizationId: string,
    shippingAddress: CheckoutAddressInput,
    billingAddress: CheckoutAddressInput,
  ) =>
    api.request<string>(`${tenant(organizationId)}/orders/checkout`, {
      method: "POST",
      body: { shippingAddress, billingAddress },
    }),
  paymentSession: (
    api: ApiClient,
    organizationId: string,
    orderId: string,
    successUrl: string,
    cancelUrl: string,
  ) =>
    api.request<PaymentSessionDto>(
      `${tenant(organizationId)}/orders/${orderId}/payment-session`,
      {
        method: "POST",
        body: {
          successUrl,
          cancelUrl,
          paymentMethodTypes: ["card", "gcash", "paymaya"],
        },
      },
    ),
  orders: (
    api: ApiClient,
    organizationId: string,
    page: number,
    signal?: AbortSignal,
  ) =>
    api.request<OrderSummariesPageDto>(
      `${tenant(organizationId)}/me/orders?page=${page}&pageSize=20`,
      { signal },
    ),
  order: (
    api: ApiClient,
    organizationId: string,
    orderId: string,
    signal?: AbortSignal,
  ) =>
    api.request<OrderDetailsDto>(
      `${tenant(organizationId)}/me/orders/${orderId}`,
      { signal },
    ),
  cancelOrder: (
    api: ApiClient,
    organizationId: string,
    orderId: string,
    reason: string,
  ) =>
    api.request<void>(
      `${tenant(organizationId)}/me/orders/${orderId}/cancellation`,
      { method: "POST", body: { reason } },
    ),
  refundItem: (
    api: ApiClient,
    organizationId: string,
    orderId: string,
    itemId: string,
    quantity: number,
    reason: string,
  ) =>
    api.request<string>(
      `${tenant(organizationId)}/me/orders/${orderId}/items/${itemId}/refunds`,
      { method: "POST", body: { quantity, reason } },
    ),
};

export function addressInput(
  address: CustomerAddressDto,
): CheckoutAddressInput {
  return {
    recipientName: address.recipientName,
    phoneNumber: address.phoneNumber,
    addressLine1: address.addressLine1,
    addressLine2: address.addressLine2,
    barangay: address.barangay,
    cityOrMunicipality: address.cityOrMunicipality,
    province: address.province,
    postalCode: address.postalCode,
    countryCode: address.countryCode,
  };
}
