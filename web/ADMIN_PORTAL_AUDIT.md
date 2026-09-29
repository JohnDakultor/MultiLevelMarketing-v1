# Admin Portal integration audit

Audited: 2026-09-29

The source of truth for this audit is `src/Web/Endpoints`, the Application request
contracts, Domain enums, and the consumers in `apps/admin-portal`. The endpoint-level
matrix remains in `ENDPOINT_PARITY.md`.

## Screen results

| Screen                     | Backend surface                                                                                 | Result                                                                                                                                      |
| -------------------------- | ----------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| Sign-in and invitations    | Identity login, current identity and invitation acceptance                                      | Wired through the shared authenticated client with field and Problem Details errors                                                         |
| Platform provisioning      | Organization creation, branding upload/update and publish                                       | Wired as a resumable multi-step workflow with duplicate-tenant protection                                                                   |
| Dashboard                  | Admin dashboard report                                                                          | Real metrics, observed time, backend alerts and operational tasks; no fabricated charts                                                     |
| Organization               | Settings, profile, branding, commerce, features, network, wallet and referral settings, domains | Wired; field values and validation are submitted to the corresponding contracts                                                             |
| Catalog and inventory      | Categories, products, product images, variants, commission profiles, inventory and history      | Wired; inspected Azure Blob image upload/replacement/removal, server inventory search/paging, optimistic-safe versions and idempotency keys |
| Orders and refunds         | Admin order search/detail, fulfillment, cancellation, payment/item refunds and reconciliation   | Wired; state-dependent actions follow Order, Payment and Fulfillment states                                                                 |
| Customers                  | Tenant customer list/search/detail and status transitions                                       | Wired; tenant isolation, audited transitions and terminal disabled state are tested                                                         |
| Agents                     | Applications, lifecycle, details, placement, pairing and wallet adjustments                     | Wired; only backend-supported lifecycle transitions are shown                                                                               |
| Compensation               | Plan list/detail/create/update/publish/retire                                                   | Wired; draft-only editing/publishing and active-only retirement                                                                             |
| Wallets and commissions    | Wallet search/status/negative filtering, entries and commission ledger filters                  | Wired with server-side paging and filtering                                                                                                 |
| Payouts                    | History, accounts, detail, approve/reject/process/reconcile                                     | Wired; account review and payout actions follow backend statuses                                                                            |
| Administrators             | Administrator list, invite, pending invitations, revocation                                     | Wired; pending-only invitation revocation and guarded submissions                                                                           |
| Reports                    | Date-ranged Admin report                                                                        | Wired; empty breakdowns are explicit and analytics are not invented                                                                         |
| Audit trail                | Tenant audit query                                                                              | Wired; immutable support identifiers and trace fields shown                                                                                 |
| Operations                 | Operational health, dead letters and replay                                                     | Wired; replay reason, loading/error state, duplicate-submit guard                                                                           |
| Notifications and sessions | Shared notification center and session management                                               | Wired through shared authenticated components                                                                                               |

All organization routes remain protected twice: the UI derives capability visibility
from the authenticated identity and organization feature flags, while the API remains
the final authorization authority. The shared API client supplies `Api-Version: 1.0`,
cookies, antiforgery acquisition for unsafe requests, and Problem Details errors.

## Backend requirements not available

The SDD describes merchant functionality as a future module extension, so it is not
part of the current Admin Portal scope or counted as a readiness gap.

1. **Payout history pagination metadata:** the Admin payout endpoint returns a raw
   list. The UI can page forward conservatively but cannot display a reliable total or
   final-page count.
2. **Administrator/audit/product list totals:** these APIs return bounded lists rather
   than a page envelope. Accurate global counts and full pagination require backend
   response metadata.
3. **Additional dashboard analytics:** the dashboard contract has operational totals,
   alerts and tasks but no time-series/network-growth/recent-activity series. No fake
   trends or charts are rendered.

## UX and reliability changes

- Shared shadcn-style primitives now use CVA variants and Lucide icons while staying
  inside the existing design-system package.
- Navigation uses Next.js links under a persistent root layout, preventing shell
  remounts and full-page navigation flashes.
- Existing data remains mounted during background reloads; initial requests use
  stable layout-shaped skeletons.
- Data tables provide sticky headers, hover hierarchy, explicit empty rows and mobile
  card rendering with accessible field labels.
- Search-heavy screens defer input-driven requests, and mutations expose per-action
  loading/disabled states, confirmation prompts and server validation errors.
- Status labels and tones are centralized from backend numeric enums instead of being
  repeated or displayed as raw numbers.
