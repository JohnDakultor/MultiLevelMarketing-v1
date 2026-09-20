// PHASE 6 PSEUDOCODE ONLY
// PostgreSQL tests: same-tenant admin adjustment changes OnHand and writes adjustment+audit.
// Duplicate key/same payload returns original result; different payload conflicts.
// Foreign Organization and non-admin are forbidden; negative resulting stock is rejected.
// Stale ExpectedVersion returns the stable concurrency conflict.
