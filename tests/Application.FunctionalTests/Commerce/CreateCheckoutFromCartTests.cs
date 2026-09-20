// PSEUDOCODE ONLY
// TEST checkout derives customer and consumes only the caller's non-empty cart.
// TEST variant IDs/prices/currency/stock/referral are revalidated in the transaction.
// TEST order/item/address snapshots and inventory changes commit atomically.
// TEST cart clears only after successful order creation.
// TEST retry does not duplicate order or reserve inventory twice.
// TEST customer cannot checkout another customer's or anonymous session's cart.
