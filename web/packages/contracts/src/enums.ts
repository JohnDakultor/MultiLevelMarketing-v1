/** Numeric API values mirrored from the backend domain enums. */
export const AgentStatus = {
  applied: 0,
  pendingApproval: 1,
  active: 2,
  inactive: 3,
  suspended: 4,
  closed: 5,
} as const;

export const ProductStatus = { draft: 0, active: 1, archived: 2 } as const;

export const OrderStatus = {
  pendingPayment: 0,
  paid: 1,
  processing: 2,
  shipped: 3,
  delivered: 4,
  cancelled: 5,
  partiallyRefunded: 6,
  refunded: 7,
} as const;

export const PaymentStatus = {
  pending: 0,
  authorized: 1,
  paid: 2,
  failed: 3,
  partiallyRefunded: 4,
  refunded: 5,
} as const;

export const FulfillmentStatus = {
  unfulfilled: 0,
  processing: 1,
  shipped: 2,
  delivered: 3,
  cancelled: 4,
  refunded: 5,
} as const;

export const PayoutStatus = {
  requested: 0,
  underReview: 1,
  approved: 2,
  processing: 3,
  paid: 4,
  rejected: 5,
  failed: 6,
  cancelled: 7,
} as const;
