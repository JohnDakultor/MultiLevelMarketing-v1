# Frontend API contract inventory

Reviewed against `src/Web/Endpoints` for Phase 12.0. The endpoint source and its
Application request/response types are authoritative. This inventory replaces the
older frontend analysis that predated the customer, Agent-operations, reporting, and
current-identity APIs.

## Transport and contract conventions

- All tenant-owned routes carry `organizationId` after host resolution. Users never
  type this value.
- `GET /api/me` returns the authenticated user, roles, Organization, Customer, and
  Agent identifiers used by the three applications.
- Cookie-authenticated unsafe methods require the token returned by
  `GET /api/security/antiforgery-token`. Bearer-authenticated requests do not request
  an antiforgery token.
- API errors use Problem Details. The frontend distinguishes 401, 403, 404, 409,
  422, 429, timeout/network failure, and 500 responses.
- Backend enums currently serialize as numbers unless an endpoint explicitly returns
  a string label.
- Pagination is not uniform. Use the exact response listed for each endpoint; do not
  synthesize totals for endpoints returning a bare list.

## Capability map

| Capability                                                           | Anonymous     | Customer | Agent                        | Organization Administrator     | Platform Administrator                        |
| -------------------------------------------------------------------- | ------------- | -------- | ---------------------------- | ------------------------------ | --------------------------------------------- |
| Resolve Organization and browse published catalog                    | Yes           | Yes      | Yes                          | Yes                            | Yes                                           |
| Use anonymous cart/referral attribution                              | Yes           | Yes      | Yes                          | Yes                            | Yes                                           |
| Manage own customer profile, addresses, and orders                   | No            | Yes      | Only when also a Customer    | Only when also a Customer      | Only when also a Customer                     |
| Apply to become an Agent                                             | No            | Yes      | Existing application/context | Explicit admin operations only | Where policy permits                          |
| Read own Agent network, compensation, wallet, and payouts            | No            | No       | Yes                          | Yes, scoped by handler policy  | Subject to handler policy                     |
| Manage catalog, Agents, compensation, refunds, payouts, and settings | No            | No       | No                           | Yes, own Organization          | Platform-scoped operations only where exposed |
| Receive PayMongo webhooks                                            | Provider only | No       | No                           | No                             | No                                            |

Frontend route hiding is a usability feature, not a security boundary. Application
authorization handlers remain authoritative for ownership and tenant isolation.

## Identity and session endpoints

| Method and route                             | Access                     | Request                                            | Response                       | Frontend owner      |
| -------------------------------------------- | -------------------------- | -------------------------------------------------- | ------------------------------ | ------------------- |
| `POST /api/Users/register`                   | Anonymous, auth rate limit | ASP.NET Identity registration request              | Identity result                | Storefront          |
| `POST /api/Users/login`                      | Anonymous, auth rate limit | ASP.NET Identity login request and transport flags | Cookie or `AuthTokenResponse`  | All apps            |
| `POST /api/Users/refresh`                    | Anonymous, auth rate limit | Refresh request                                    | `AuthTokenResponse`            | Shared auth         |
| `POST /api/Users/logout`                     | Authenticated              | Empty JSON object                                  | 200                            | Shared auth         |
| `GET /api/Users/manage/info`                 | Authenticated              | None                                               | ASP.NET Identity info response | Storefront security |
| `POST /api/Users/manage/info`                | Authenticated              | ASP.NET Identity info request                      | Identity info response         | Storefront security |
| `POST /api/Users/manage/2fa`                 | Authenticated              | ASP.NET Identity MFA request                       | MFA state/recovery response    | Storefront security |
| `GET /api/me`                                | Authenticated              | None                                               | `CurrentUserDto`               | Shared auth         |
| `GET /api/me/sessions`                       | Authenticated              | None                                               | Session list                   | Shared auth         |
| `POST /api/me/session/revoke`                | Authenticated              | `RevokeCurrentSessionRequest`                      | 204                            | Shared auth         |
| `GET /api/security/antiforgery-token`        | Cookie-authenticated       | None                                               | `AntiforgeryTokenResponse`     | Shared API client   |
| `POST /api/administrator-invitations/accept` | Anonymous                  | `AcceptAdministratorInvitationRequest`             | 204                            | Admin auth flow     |

`MapIdentityApi` also exposes confirmation, password-reset, and account-management
operations under `/api/Users`. Consumers use the generated OpenAPI contract for those
framework-owned payloads.

## Organization and public catalog endpoints

| Method and route                                                           | Access                 | Request/query               | Response                             | Frontend owner         |
| -------------------------------------------------------------------------- | ---------------------- | --------------------------- | ------------------------------------ | ---------------------- |
| `GET /api/organizations/public-config`                                     | Anonymous              | Request hostname            | `PublicOrganizationConfigDto` or 404 | All apps               |
| `GET /api/organizations/{slug}/public-config`                              | Anonymous fallback     | Slug route                  | `PublicOrganizationConfigDto` or 404 | Local development only |
| `POST /api/organizations`                                                  | Platform Administrator | `CreateOrganizationRequest` | Created Organization ID              | Admin/platform flow    |
| `GET /api/organizations/{organizationId}/categories`                       | Anonymous              | None                        | `IReadOnlyList<CategoryDto>`         | Storefront             |
| `GET /api/organizations/{organizationId}/products?page&pageSize`           | Anonymous              | Page arguments              | Bare `IReadOnlyList<ProductDto>`     | Storefront             |
| `GET /api/organizations/{organizationId}/products/{slug}`                  | Anonymous              | Product slug                | `ProductDetailDto`                   | Storefront             |
| `GET /api/organizations/{organizationId}/referrals/store/{referralCode}`   | Anonymous              | Paging/filter query         | `AgentStorefrontDto`                 | Storefront             |
| `GET /api/organizations/{organizationId}/referrals/resolve/{referralCode}` | Anonymous              | Referral code               | `ReferralAttributionDto` and cookie  | Storefront             |

## Cart, checkout, customer, and order endpoints

| Method and route                                                                           | Access              | Request/query                  | Response                            | Frontend owner   |
| ------------------------------------------------------------------------------------------ | ------------------- | ------------------------------ | ----------------------------------- | ---------------- |
| `GET /api/organizations/{organizationId}/cart`                                             | Anonymous/Customer  | Session/current user           | `CartDto`                           | Storefront       |
| `POST /api/organizations/{organizationId}/cart/items`                                      | Anonymous/Customer  | `AddCartItemRequest`           | Created Cart ID                     | Storefront       |
| `PUT /api/organizations/{organizationId}/cart/items/{cartItemId}`                          | Anonymous/Customer  | `UpdateCartItemRequest`        | `CartDto`                           | Storefront       |
| `DELETE /api/organizations/{organizationId}/cart/items/{cartItemId}`                       | Anonymous/Customer  | Owned cart item                | `CartDto`                           | Storefront       |
| `PUT /api/organizations/{organizationId}/cart/referral`                                    | Anonymous/Customer  | `ApplyReferralCodeRequest`     | `CartDto`                           | Storefront       |
| `POST /api/organizations/{organizationId}/orders/checkout`                                 | Authenticated owner | `CreateCheckoutRequest`        | Created Order ID                    | Storefront       |
| `POST /api/organizations/{organizationId}/orders/{orderId}/payment-session`                | Owner/Admin         | `CreatePaymentSessionRequest`  | `PaymentSessionDto`                 | Storefront/Admin |
| `GET /api/organizations/{organizationId}/me/profile`                                       | Current Customer    | None                           | `CustomerProfileDto`                | Storefront       |
| `PUT /api/organizations/{organizationId}/me/profile`                                       | Current Customer    | `UpdateCustomerProfileRequest` | `CustomerProfileDto`                | Storefront       |
| `GET /api/organizations/{organizationId}/me/addresses`                                     | Current Customer    | None                           | `IReadOnlyList<CustomerAddressDto>` | Storefront       |
| `POST /api/organizations/{organizationId}/me/addresses`                                    | Current Customer    | `CustomerAddressRequest`       | Created Address ID                  | Storefront       |
| `PUT /api/organizations/{organizationId}/me/addresses/{addressId}`                         | Address owner       | `CustomerAddressRequest`       | 204                                 | Storefront       |
| `DELETE /api/organizations/{organizationId}/me/addresses/{addressId}`                      | Address owner       | None                           | 204                                 | Storefront       |
| `GET /api/organizations/{organizationId}/me/orders?page&pageSize&status`                   | Current Customer    | Page/status query              | `OrderSummariesPageDto`             | Storefront       |
| `GET /api/organizations/{organizationId}/me/orders/{orderId}`                              | Order owner         | None                           | `OrderDetailsDto`                   | Storefront       |
| `POST /api/organizations/{organizationId}/me/orders/{orderId}/cancellation`                | Order owner         | `RequestCancellationRequest`   | 204                                 | Storefront       |
| `POST /api/organizations/{organizationId}/me/orders/{orderId}/items/{orderItemId}/refunds` | Owner, rate limited | `RequestCustomerRefundRequest` | Created Refund ID                   | Storefront       |

Checkout derives Customer and cart ownership on the backend. The frontend never sends
authoritative item price, BV, Customer ID, Agent ID, or Organization ownership data.

## Current Agent and Agent workspace endpoints

| Method and route                                                        | Access             | Request/query            | Response                        | Frontend owner   |
| ----------------------------------------------------------------------- | ------------------ | ------------------------ | ------------------------------- | ---------------- |
| `POST /api/organizations/{organizationId}/agents/applications`          | Authenticated user | Optional sponsor input   | Created ID                      | Storefront/Agent |
| `GET /api/organizations/{organizationId}/agent/context`                 | Authenticated      | None                     | `CurrentAgentContextDto` or 404 | Agent            |
| `GET /api/organizations/{organizationId}/agent/application`             | Authenticated      | None                     | `AgentApplicationDto` or 404    | Agent            |
| `GET /api/organizations/{organizationId}/agent/profile`                 | Agent              | None                     | `AgentProfileDto`               | Agent            |
| `GET /api/organizations/{organizationId}/agent/qualification`           | Agent              | None                     | `AgentQualificationStatusDto`   | Agent            |
| `PUT /api/organizations/{organizationId}/agent/preferred-leg`           | Agent              | `SetPreferredLegRequest` | 204                             | Agent            |
| `GET /api/organizations/{organizationId}/agent/referral`                | Agent              | None                     | `ReferralLinkDto`               | Agent            |
| `POST /api/organizations/{organizationId}/agent/referral/product-links` | Agent              | Product ID               | `ProductReferralLinkDto`        | Agent            |
| `GET /api/organizations/{organizationId}/agent/sales`                   | Agent              | Paging/filter query      | `AttributedOrdersPageDto`       | Agent            |
| `GET /api/organizations/{organizationId}/agent/sales/{orderId}`         | Agent              | Order ID                 | `AttributedOrderDetailsDto`     | Agent            |
| `GET /api/organizations/{organizationId}/agent/sales/products`          | Agent              | Paging/filter query      | `AgentProductSalesPageDto`      | Agent            |
| `GET /api/organizations/{organizationId}/reports/agent?from&to`         | Agent              | Date range               | `AgentReportDto`                | Agent            |

Older Agent-ID routes remain available for Agent/Admin use, but Application
authorization verifies ownership and visibility. Agent Portal gets `agentId` from
`GET /api/me`; it never asks the user to enter one.

| Route family                | Operations                                                 | Response contracts                         |
| --------------------------- | ---------------------------------------------------------- | ------------------------------------------ |
| `/agents/{agentId}/network` | tree, downline, children, ancestors, recruits, leg summary | Network DTOs                               |
| `/agents/{agentId}/finance` | wallet, earnings, commissions, detail, BV, payout request  | Finance DTOs/created payout ID             |
| `/agents/{agentId}/wallet`  | summary and paged ledger                                   | `WalletSummaryDto`, `WalletEntriesPageDto` |
| `/agents/{agentId}/payouts` | history, details, cancel, accounts, submit/default         | Payout/account DTOs                        |
| `/agents/{agentId}/pairing` | pairing history                                            | `List<BinaryPairingRunDto>`                |

## Administrator endpoints

These routes require the Administrator role unless noted. Application authorization
also verifies permission for the route Organization.

### Organization and administrators

| Method and route                                                                            | Request/query                        | Response                          |
| ------------------------------------------------------------------------------------------- | ------------------------------------ | --------------------------------- |
| `GET /api/organizations/{organizationId}/admin/settings`                                    | None                                 | `AdminOrganizationSettingsDto`    |
| `PUT /api/organizations/{organizationId}/admin/profile`                                     | `UpdateOrganizationProfileRequest`   | 204                               |
| `PUT /api/organizations/{organizationId}/admin/commerce-settings`                           | `UpdateCommerceSettingsRequest`      | 204                               |
| `POST /api/organizations/{organizationId}/admin/branding/publish`                           | None                                 | 204                               |
| `POST /api/organizations/{organizationId}/admin/branding/assets/{assetKind}`                | Multipart upload                     | Stored object result              |
| `POST /api/organizations/{organizationId}/admin/domains`                                    | `ConfigureOrganizationDomainRequest` | Created Domain ID                 |
| `DELETE /api/organizations/{organizationId}/admin/domains/{organizationDomainId}`           | None                                 | 204                               |
| `PUT /api/organizations/{organizationId}/network-settings`                                  | `UpdateNetworkSettingsRequest`       | 204                               |
| `GET/PUT /api/organizations/{organizationId}/wallet-settings`                               | None / update request                | `WalletSettingsDto` / 204         |
| `PUT /api/organizations/{organizationId}/branding`                                          | `UpdateBrandingRequest`              | 204                               |
| `PUT /api/organizations/{organizationId}/feature-settings`                                  | `UpdateFeatureSettingsRequest`       | 204                               |
| `GET /api/organizations/{organizationId}/administrators`                                    | None                                 | `IReadOnlyList<AdministratorDto>` |
| `GET /api/organizations/{organizationId}/administrators/invitations`                        | Status query                         | Invitation DTO list               |
| `POST /api/organizations/{organizationId}/administrators/invitations`                       | Invitation request                   | Created Invitation ID             |
| `POST /api/organizations/{organizationId}/administrators/invitations/{invitationId}/revoke` | Revocation request                   | 204                               |
| `POST /api/organizations/{organizationId}/administrators/{administratorUserId}/revoke-role` | None                                 | 204                               |

### Administration route families

| Route family                           | Operations                                                    | Contracts                            |
| -------------------------------------- | ------------------------------------------------------------- | ------------------------------------ |
| `/products`                            | create product/profile and assign commission profile          | Request records -> IDs/204           |
| `/admin/products`                      | paged list, details, update, publish, archive, categories     | Admin product/category DTOs          |
| `/admin/products/{productId}/variants` | create, update, archive                                       | Request records -> ID/204            |
| `/admin/inventory`                     | paged inventory, variant history, adjustment                  | Inventory page/history DTOs          |
| `/admin/agents`                        | applications, Agent list/details, move placement              | Agent admin page/detail DTOs         |
| `/agents/{agentId}`                    | place/auto-place, approve/reject, activate/suspend/reactivate | IDs/204                              |
| `/admin/compensation`                  | list/create/publish/retire plans                              | `CommissionPlanDto` list and results |
| `/agents/{agentId}/pairing/runs`       | process pairing                                               | pairing result DTO                   |
| `/admin/payouts`                       | history/details and payout/account decisions                  | Payout DTOs/204                      |
| `/orders`                              | payment/refund reconciliation                                 | IDs/204                              |
| `/orders/{orderId}/refunds`            | request/history/reconciliation                                | Refund DTOs/IDs/204                  |
| `/agents/{agentId}/wallet/adjustments` | wallet adjustment                                             | Created Wallet Entry ID              |
| `/admin/audit-trail`                   | paged filters                                                 | Audit trail result                   |
| `/admin/operational-health`            | health snapshot                                               | `OperationalHealthDto`               |
| `/admin/messaging/dead-letters`        | page and replay                                               | `DeadLetterMessagesPageDto`/204      |
| `/reports/admin?from&to`               | date range report                                             | `AdminReportDto`                     |
| `/reports/admin/dashboard`             | actionable aggregate                                          | `AdminDashboardDto`                  |

### Administrator order discovery

| Method and route                                                 | Request/query                                                        | Response               |
| ---------------------------------------------------------------- | -------------------------------------------------------------------- | ---------------------- |
| `GET /api/organizations/{organizationId}/admin/orders`           | Page, page size, search, order/payment status, created-from/to dates | `AdminOrdersPageDto`   |
| `GET /api/organizations/{organizationId}/admin/orders/{orderId}` | Tenant-scoped order ID                                               | `AdminOrderDetailsDto` |

The list searches order numbers and customer display names. Details include immutable
order/item snapshots, payments, payment refunds, and item-refund history. Both queries
implement `IOrganizationAdminRequest`; the routes additionally require the
Administrator role.

## Notifications and provider-only routes

| Method and route                                                                 | Access                  | Response/purpose            |
| -------------------------------------------------------------------------------- | ----------------------- | --------------------------- |
| `GET /api/organizations/{organizationId}/me/notifications`                       | Current user            | `NotificationPageDto`       |
| `PUT /api/organizations/{organizationId}/me/notifications/{notificationId}/read` | Owner                   | 204                         |
| `POST /api/webhooks/paymongo`                                                    | Signed provider webhook | Browser UI never calls this |
| `POST /api/webhooks/paymongo/transfers`                                          | Signed provider webhook | Browser UI never calls this |

## Screen ownership

| Application  | Owned workflows                                                                                                                                               |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Storefront   | host branding, catalog, product details, referral storefront, cart, checkout/payment return, customer profile/addresses/orders                                |
| Agent Portal | Agent context/profile, dashboard report, network, sales, qualification, referrals, commissions, wallet, payouts                                               |
| Admin Portal | Organization settings, administrators, catalog/inventory, Agents/placement, compensation, orders/refunds/payments, payouts, messaging, reports, health, audit |

## Current API gaps

### RESOLVED API GAP: administrator order discovery

The backend now exposes tenant-authorized, paged Administrator order search and a
details endpoint containing payments and refund history. PostgreSQL functional tests
cover pagination, search, nested financial details, route authorization, and tenant
isolation. The Admin Portal now consumes both endpoints and exposes financial actions
from selected order/payment/refund records; administrators are not asked to enter raw
record IDs.

### API GAP: server-side storefront search and category filter

Frontend requirement: searchable and category-filtered public catalog browsing.

Current API limitation: the public products endpoint supports page and page size only;
its DTO also omits category metadata. Search can therefore apply only to the loaded
page and categories cannot safely filter the result.

Recommended backend change: add optional search and category parameters and return
normal page metadata.

### API GAP: cart price-change comparison and shipment tracking

Frontend requirement: explain when a cart price changed since it was added and show
carrier/tracking progress on customer order details.

Current API limitation: `CartItemDto` contains only the current price, while
`OrderDetailsDto` exposes fulfillment status but no carrier, tracking number, or
tracking events. The UI displays current authoritative values without inventing a
comparison or shipment timeline.

Recommended backend change: return the prior cart price/change indicator and a
privacy-safe fulfillment tracking contract.

### API GAP: complete Organization settings read model

Frontend requirement: edit network and referral settings from their persisted values
and assign existing product commission profiles.

Current API limitation: network settings have a write endpoint but are absent from
`AdminOrganizationSettingsDto`; referral settings and product commission profiles
have no Administrator list/read endpoint.

Recommended backend change: include network, referral, and wallet settings in one
Administrator settings read model and expose a commission-profile list.

### API GAP: Agent network filtering and payout eligibility explanation

Frontend requirement: server-filter very large networks and explain every failed
payout eligibility rule.

Current API limitation: network endpoints support depth but not search/leg/status
filters, and the payout command evaluates eligibility without exposing a read-only
eligibility decision DTO.

Recommended backend change: add documented network filters/page metadata and a
current-Agent payout eligibility endpoint returning stable reason codes and messages.

### RESOLVED API GAP: current session revocation identifier

Frontend requirement: revoke the active session through
`POST /api/me/session/revoke`.

Cookie-authenticated browsers now receive a protected session cookie backed by an
`AuthenticationSessions` record. `GET /api/me/sessions` returns only the current
user's sessions and identifies the current browser. Revoking the current record also
clears ASP.NET Identity authentication; revoking another record causes that browser
to receive 401 on its next request. Bearer transport remains outside this cookie
session mechanism.

### API GAP: inconsistent list pagination

Frontend requirement: reliable page count and navigation for all large lists.

Current API limitation: some endpoints accept page arguments but return a bare list,
while newer endpoints return page metadata.

Recommended backend change: migrate list endpoints to a shared page contract with
items, page, page size, total count, total pages, and next/previous indicators.

### API GAP: report time series

Frontend requirement: trend charts at a documented granularity.

Current API limitation: reports provide totals and breakdowns but no daily/weekly
time-series buckets.

Recommended backend change: add time-series buckets only when product requirements
define the range and granularity. The UI must not fabricate trends.

### Deployment requirement: hostname forwarding

Organization resolution depends on the frontend hostname reaching the backend as the
effective forwarded host. Each Next.js deployment or trusted edge proxy must forward
the original host. The slug endpoint remains a localhost/development fallback and is
not an ordinary-user setting.
