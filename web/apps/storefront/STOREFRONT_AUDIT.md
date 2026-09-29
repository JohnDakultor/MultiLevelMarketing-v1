# Storefront backend integration audit

Last reviewed: 2026-09-29

## Integrated capabilities

| Capability                            | Backend route                                                                                                         | Frontend                                                                 |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| Organization storefront configuration | `GET /api/organizations/{slug}/public-config` and host resolution                                                     | Organization provider and server catalog bootstrap                       |
| Categories                            | `GET /api/organizations/{organizationId}/categories`                                                                  | Home collection discovery                                                |
| Product discovery                     | `GET /api/organizations/{organizationId}/products` with page, search, category, price, availability, and sort filters | Debounced server-side search/filter/sort, paged results, and total count |
| Product details and variants          | `GET /api/organizations/{organizationId}/products/{slug}`                                                             | Product detail, stock-aware variants, quantity limits                    |
| Agent referral storefront             | `GET /api/organizations/{organizationId}/referrals/store/{code}`                                                      | Referral collection                                                      |
| Referral resolution                   | `GET /api/organizations/{organizationId}/referrals/resolve/{code}`                                                    | Referral attribution flow                                                |
| Anonymous cart                        | `GET/POST/PUT/DELETE /api/organizations/{organizationId}/cart`                                                        | Cart viewing and item mutations                                          |
| Cart referral                         | `PUT /api/organizations/{organizationId}/cart/referral`                                                               | Referral entry in cart                                                   |
| Customer profile                      | `GET/PUT /api/organizations/{organizationId}/me/profile`                                                              | Profile screen                                                           |
| Customer addresses                    | `GET/POST/PUT/DELETE /api/organizations/{organizationId}/me/addresses`                                                | Address management and checkout selection                                |
| Checkout                              | `POST /api/organizations/{organizationId}/orders/checkout`                                                            | Authenticated address-snapshot checkout                                  |
| Payment session                       | `POST /api/organizations/{organizationId}/orders/{orderId}/payment-session`                                           | Provider redirect and return screen                                      |
| Customer orders                       | `GET /api/organizations/{organizationId}/me/orders`                                                                   | Paged history with server-side status filter                             |
| Order details                         | `GET /api/organizations/{organizationId}/me/orders/{orderId}`                                                         | Totals, addresses, items, statuses                                       |
| Customer cancellation                 | `POST /api/organizations/{organizationId}/me/orders/{orderId}/cancellation`                                           | Eligibility-controlled cancellation                                      |
| Customer item refund                  | `POST /api/organizations/{organizationId}/me/orders/{orderId}/items/{itemId}/refunds`                                 | Eligibility-controlled refund request                                    |
| Identity security                     | `/api/Users/manage/info`, `/api/Users/manage/2fa`, session routes                                                     | Password, email, MFA, sessions                                           |

## Backend gaps intentionally not fabricated in the UI

- No dedicated brand aggregate/filter; category, price, availability, and supported sorting are server-side.
- No wishlist/favorites endpoints.
- No product review or rating endpoints.
- No promotion, voucher, or discount-entry endpoints for customers.
- No recommendation, popularity, best-seller, or new-arrival signals.
- No customer-selectable shipping methods or shipping quote endpoint.
- No customer reorder endpoint.

The storefront must add these experiences only after corresponding backend contracts exist. Until then, the backend remains authoritative for availability, checkout totals, order actions, payment, cancellation, and refunds.
