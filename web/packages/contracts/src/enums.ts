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

export const CommissionPlanStatus = {
  draft: 0,
  active: 1,
  retired: 2,
} as const;

export const PayoutVerificationStatus = {
  unverified: 0,
  pending: 1,
  verified: 2,
  rejected: 3,
} as const;

export const AdministratorInvitationStatus = {
  pending: 0,
  accepted: 1,
  revoked: 2,
  expired: 3,
} as const;

export const WalletStatus = { active: 0, held: 1, closed: 2 } as const;

export const WalletEntryType = {
  pendingCredit: 0,
  availableCredit: 1,
  hold: 2,
  debit: 3,
  payout: 4,
  adjustment: 5,
  reversal: 6,
} as const;

export const CommissionType = {
  directSale: 0,
  binaryPairing: 1,
  bonus: 2,
  adjustment: 3,
  reversal: 4,
} as const;

export const CommissionStatus = {
  pending: 0,
  available: 1,
  paid: 2,
  reversed: 3,
  held: 4,
} as const;

export const BinaryVolumeEntryType = {
  credit: 0,
  reversal: 1,
  adjustment: 2,
  pairConsumption: 3,
  expiry: 4,
} as const;

export const OrderItemRefundStatus = {
  pending: 0,
  reversed: 1,
  reversalFailed: 2,
} as const;

export const PaymentRefundStatus = {
  pending: 0,
  succeeded: 1,
  failed: 2,
} as const;
