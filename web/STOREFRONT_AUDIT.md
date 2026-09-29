# Storefront integration audit

Audited: 2026-09-29

Public catalog discovery now performs search, category, price, availability, sorting,
counting, and pagination in the backend database query. The Storefront sends debounced
search and server filter parameters, keeps the prior response mounted during refresh,
and displays real total-count metadata. Current-page-only filtering has been removed.

Status: **Implemented and covered by backend functional and frontend contract tests.**
Checkout, PayMongo redirects/webhooks, SMTP links, and refund callbacks still require
the configured deployed UAT environment for real-provider verification.
