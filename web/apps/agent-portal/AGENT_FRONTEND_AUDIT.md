# Agent Frontend API audit

This document records the backend contracts used by the Agent Portal. The
backend remains the source of truth for identity, authorization, lifecycle
state, tenant scope, eligibility, and financial values.

## Integrated Agent capabilities

| Area                      | Backend contract                                                                            | Agent Portal surface                           |
| ------------------------- | ------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| Identity account          | `POST /api/Users/register`, cookie login/logout, session APIs                               | `/register`, `/sign-in`, `/security`           |
| Agent onboarding          | `POST /api/organizations/{organizationId}/agents/applications`; `GET .../agent/application` | Portal access gate and application status view |
| Current Agent permissions | `GET .../agent/context`                                                                     | Payout and referral action eligibility         |
| Profile and qualification | `GET .../agent/profile`; `GET .../agent/qualification`; `PUT .../agent/preferred-leg`       | `/profile`, dashboard                          |
| Network                   | tree, downline, children, ancestors, recruits, and leg-summary routes                       | `/network`                                     |
| Attributed sales          | paged orders, order details, and paged product summaries                                    | `/sales` with server-side status/date filters  |
| Reporting                 | `GET .../reports/agent?from&to`                                                             | dashboard 30-day summary                       |
| Commissions and earnings  | earnings, commission history/details, binary volume, volume ledger, pairing history         | `/earnings`                                    |
| Wallet transactions       | wallet summary and paged ledger with entry-type/date filters                                | `/wallet`, `/transactions`                     |
| Payouts                   | account registration/submission/default; request/history/details/cancel                     | `/payouts` with domain-state actions           |
| Referrals                 | current link, product link, dashboard, code regeneration, catalog                           | `/referrals`                                   |
| Notifications             | organization-scoped notification APIs                                                       | `/notifications`                               |

Agent lifecycle transitions such as approval, placement, activation,
suspension, and reactivation are deliberately not exposed here because the
backend authorizes them only for administrators.

## Backend gaps

The following requested product areas have no Agent-authorized backend
contract and therefore are not fabricated in the UI:

- A dedicated Agent customer directory or customer-management endpoint.
- A unified cross-ledger transaction endpoint. The Transactions screen uses
  the canonical wallet ledger; commissions and binary-volume entries remain
  in Earnings so their source-specific details are preserved.
- Server-side free-text search or sort controls for wallet entries,
  commissions, payouts, or attributed sales. Available server pagination and
  enum/date filters are wired.
- Agent-editable identity/profile fields beyond preferred placement leg.
- Agent self-placement, self-approval, or self-activation.
- A sponsor lookup by code/name for applications. The application contract
  accepts only an optional active Sponsor Agent UUID.
- Dedicated analytics for team sales, trends, alerts, or arbitrary report
  exports. The dashboard displays only values returned by the existing Agent
  report, qualification, and earnings APIs.

Public commerce referral destinations still point to the Storefront by
design. The Agent Portal does not wait for the Storefront at startup and no
Agent account, application, financial, or network workflow is implemented in
the Storefront.

The legacy Agent-ID referral-link read route is intentionally not called by
the Agent Portal. It exposes the same data as the safer current-Agent route
used by `agentApi.referral`, which derives Agent identity from the authenticated
session instead of accepting it as frontend input.
