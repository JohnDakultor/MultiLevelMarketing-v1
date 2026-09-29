import {
  AgentStatus,
  AdministratorInvitationStatus,
  CommissionStatus,
  CommissionPlanStatus,
  FulfillmentStatus,
  OrderStatus,
  PaymentStatus,
  PaymentRefundStatus,
  PayoutStatus,
  PayoutVerificationStatus,
  ProductStatus,
  OrderItemRefundStatus,
  WalletStatus,
} from "@modular-mlm/contracts";
import type { FeedbackTone } from "@modular-mlm/design-system";

export interface StatusPresentation {
  label: string;
  tone: FeedbackTone;
}

const unknown = (value: number): StatusPresentation => ({
  label: `Unknown (${value})`,
  tone: "neutral",
});

export function agentStatus(value: number): StatusPresentation {
  switch (value) {
    case AgentStatus.applied:
      return { label: "Applied", tone: "info" };
    case AgentStatus.pendingApproval:
      return { label: "Pending approval", tone: "warning" };
    case AgentStatus.active:
      return { label: "Active", tone: "success" };
    case AgentStatus.inactive:
      return { label: "Inactive", tone: "neutral" };
    case AgentStatus.suspended:
      return { label: "Suspended", tone: "danger" };
    case AgentStatus.closed:
      return { label: "Closed", tone: "danger" };
    default:
      return unknown(value);
  }
}

export function productStatus(value: number): StatusPresentation {
  switch (value) {
    case ProductStatus.draft:
      return { label: "Draft", tone: "neutral" };
    case ProductStatus.active:
      return { label: "Published", tone: "success" };
    case ProductStatus.archived:
      return { label: "Archived", tone: "danger" };
    default:
      return unknown(value);
  }
}

export function orderStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [OrderStatus.pendingPayment]: { label: "Pending payment", tone: "warning" },
    [OrderStatus.paid]: { label: "Paid", tone: "info" },
    [OrderStatus.processing]: { label: "Processing", tone: "info" },
    [OrderStatus.shipped]: { label: "Shipped", tone: "info" },
    [OrderStatus.delivered]: { label: "Delivered", tone: "success" },
    [OrderStatus.cancelled]: { label: "Cancelled", tone: "danger" },
    [OrderStatus.partiallyRefunded]: {
      label: "Partially refunded",
      tone: "warning",
    },
    [OrderStatus.refunded]: { label: "Refunded", tone: "neutral" },
  };
  return values[value] ?? unknown(value);
}

export function paymentStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [PaymentStatus.pending]: { label: "Pending", tone: "warning" },
    [PaymentStatus.authorized]: { label: "Authorized", tone: "info" },
    [PaymentStatus.paid]: { label: "Paid", tone: "success" },
    [PaymentStatus.failed]: { label: "Failed", tone: "danger" },
    [PaymentStatus.partiallyRefunded]: {
      label: "Partially refunded",
      tone: "warning",
    },
    [PaymentStatus.refunded]: { label: "Refunded", tone: "neutral" },
  };
  return values[value] ?? unknown(value);
}

export function fulfillmentStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [FulfillmentStatus.unfulfilled]: { label: "Unfulfilled", tone: "warning" },
    [FulfillmentStatus.processing]: { label: "Processing", tone: "info" },
    [FulfillmentStatus.shipped]: { label: "Shipped", tone: "info" },
    [FulfillmentStatus.delivered]: { label: "Delivered", tone: "success" },
    [FulfillmentStatus.cancelled]: { label: "Cancelled", tone: "danger" },
    [FulfillmentStatus.refunded]: { label: "Refunded", tone: "neutral" },
  };
  return values[value] ?? unknown(value);
}

export function payoutStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [PayoutStatus.requested]: { label: "Requested", tone: "warning" },
    [PayoutStatus.underReview]: { label: "Under review", tone: "warning" },
    [PayoutStatus.approved]: { label: "Approved", tone: "info" },
    [PayoutStatus.processing]: { label: "Processing", tone: "info" },
    [PayoutStatus.paid]: { label: "Paid", tone: "success" },
    [PayoutStatus.rejected]: { label: "Rejected", tone: "danger" },
    [PayoutStatus.failed]: { label: "Failed", tone: "danger" },
    [PayoutStatus.cancelled]: { label: "Cancelled", tone: "neutral" },
  };
  return values[value] ?? unknown(value);
}

export function payoutAccountStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [PayoutVerificationStatus.unverified]: {
      label: "Not submitted",
      tone: "neutral",
    },
    [PayoutVerificationStatus.pending]: {
      label: "Pending verification",
      tone: "warning",
    },
    [PayoutVerificationStatus.verified]: {
      label: "Verified",
      tone: "success",
    },
    [PayoutVerificationStatus.rejected]: {
      label: "Rejected",
      tone: "danger",
    },
  };
  return values[value] ?? unknown(value);
}

export function commissionPlanStatus(value: number): StatusPresentation {
  switch (value) {
    case CommissionPlanStatus.draft:
      return { label: "Draft", tone: "neutral" };
    case CommissionPlanStatus.active:
      return { label: "Active", tone: "success" };
    case CommissionPlanStatus.retired:
      return { label: "Retired", tone: "neutral" };
    default:
      return unknown(value);
  }
}

export function administratorInvitationStatus(
  value: number,
): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [AdministratorInvitationStatus.pending]: {
      label: "Pending",
      tone: "warning",
    },
    [AdministratorInvitationStatus.accepted]: {
      label: "Accepted",
      tone: "success",
    },
    [AdministratorInvitationStatus.revoked]: {
      label: "Revoked",
      tone: "danger",
    },
    [AdministratorInvitationStatus.expired]: {
      label: "Expired",
      tone: "neutral",
    },
  };
  return values[value] ?? unknown(value);
}

export function walletStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [WalletStatus.active]: { label: "Active", tone: "success" },
    [WalletStatus.held]: { label: "Held", tone: "warning" },
    [WalletStatus.closed]: { label: "Closed", tone: "neutral" },
  };
  return values[value] ?? unknown(value);
}

export const walletEntryType = (value: number) =>
  [
    "Pending credit",
    "Available credit",
    "Hold",
    "Debit",
    "Payout",
    "Adjustment",
    "Reversal",
  ][value] ?? `Unknown (${value})`;

export const commissionType = (value: number) =>
  ["Direct sale", "Binary pairing", "Bonus", "Adjustment", "Reversal"][value] ??
  `Unknown (${value})`;

export function commissionStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [CommissionStatus.pending]: { label: "Pending", tone: "warning" },
    [CommissionStatus.available]: { label: "Available", tone: "success" },
    [CommissionStatus.paid]: { label: "Paid", tone: "success" },
    [CommissionStatus.reversed]: { label: "Reversed", tone: "neutral" },
    [CommissionStatus.held]: { label: "Held", tone: "warning" },
  };
  return values[value] ?? unknown(value);
}

export function orderItemRefundStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [OrderItemRefundStatus.pending]: { label: "Pending", tone: "warning" },
    [OrderItemRefundStatus.reversed]: { label: "Reversed", tone: "success" },
    [OrderItemRefundStatus.reversalFailed]: {
      label: "Reversal failed",
      tone: "danger",
    },
  };
  return values[value] ?? unknown(value);
}

export function paymentRefundStatus(value: number): StatusPresentation {
  const values: Record<number, StatusPresentation> = {
    [PaymentRefundStatus.pending]: { label: "Pending", tone: "warning" },
    [PaymentRefundStatus.succeeded]: { label: "Succeeded", tone: "success" },
    [PaymentRefundStatus.failed]: { label: "Failed", tone: "danger" },
  };
  return values[value] ?? unknown(value);
}
