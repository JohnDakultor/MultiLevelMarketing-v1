// PHASE 6 PSEUDOCODE ONLY
// Seed one available unit and two customers with separate carts.
// Start checkout commands concurrently against PostgreSQL using independent scopes.
// Assert exactly one checkout succeeds, one receives stable conflict/out-of-stock response,
// one active reservation exists, and OnHand/Reserved/Available never become negative.
