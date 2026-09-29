import {
  AgentStatus,
  BinaryVolumeEntryType,
  CommissionStatus,
  CommissionType,
  FulfillmentStatus,
  OrderStatus,
  PaymentStatus,
  PayoutStatus,
  PayoutVerificationStatus,
  WalletEntryType,
} from "@modular-mlm/contracts";
import { StatusBadge, type FeedbackTone } from "@modular-mlm/design-system";

type StatusDefinition = { label: string; tone: FeedbackTone };

const unknown = (value: number): StatusDefinition => ({
  label: `Unknown (${value})`,
  tone: "neutral",
});

const agentStatuses: Record<number, StatusDefinition> = {
  [AgentStatus.applied]: { label: "Applied", tone: "info" },
  [AgentStatus.pendingApproval]: { label: "Pending approval", tone: "warning" },
  [AgentStatus.active]: { label: "Active", tone: "success" },
  [AgentStatus.inactive]: { label: "Inactive", tone: "neutral" },
  [AgentStatus.suspended]: { label: "Suspended", tone: "danger" },
  [AgentStatus.closed]: { label: "Closed", tone: "danger" },
};

const payoutStatuses: Record<number, StatusDefinition> = {
  [PayoutStatus.requested]: { label: "Requested", tone: "info" },
  [PayoutStatus.underReview]: { label: "Under review", tone: "warning" },
  [PayoutStatus.approved]: { label: "Approved", tone: "success" },
  [PayoutStatus.processing]: { label: "Processing", tone: "info" },
  [PayoutStatus.paid]: { label: "Paid", tone: "success" },
  [PayoutStatus.rejected]: { label: "Rejected", tone: "danger" },
  [PayoutStatus.failed]: { label: "Failed", tone: "danger" },
  [PayoutStatus.cancelled]: { label: "Cancelled", tone: "neutral" },
};

const accountStatuses: Record<number, StatusDefinition> = {
  [PayoutVerificationStatus.unverified]: {
    label: "Unverified",
    tone: "neutral",
  },
  [PayoutVerificationStatus.pending]: {
    label: "Pending verification",
    tone: "warning",
  },
  [PayoutVerificationStatus.verified]: { label: "Verified", tone: "success" },
  [PayoutVerificationStatus.rejected]: { label: "Rejected", tone: "danger" },
};

const orderStatuses: Record<number, StatusDefinition> = {
  [OrderStatus.pendingPayment]: { label: "Pending payment", tone: "warning" },
  [OrderStatus.paid]: { label: "Paid", tone: "success" },
  [OrderStatus.processing]: { label: "Processing", tone: "info" },
  [OrderStatus.shipped]: { label: "Shipped", tone: "info" },
  [OrderStatus.delivered]: { label: "Delivered", tone: "success" },
  [OrderStatus.cancelled]: { label: "Cancelled", tone: "neutral" },
  [OrderStatus.partiallyRefunded]: {
    label: "Partially refunded",
    tone: "warning",
  },
  [OrderStatus.refunded]: { label: "Refunded", tone: "neutral" },
};

const commissionStatuses: Record<number, StatusDefinition> = {
  [CommissionStatus.pending]: { label: "Pending", tone: "warning" },
  [CommissionStatus.available]: { label: "Available", tone: "success" },
  [CommissionStatus.paid]: { label: "Paid", tone: "success" },
  [CommissionStatus.reversed]: { label: "Reversed", tone: "danger" },
  [CommissionStatus.held]: { label: "Held", tone: "warning" },
};

const labels = (values: Record<string, number>) =>
  Object.fromEntries(
    Object.entries(values).map(([key, value]) => [
      value,
      key
        .replace(/([A-Z])/g, " $1")
        .replace(/^./, (letter) => letter.toUpperCase()),
    ]),
  ) as Record<number, string>;

const commissionTypes = labels(CommissionType);
const walletEntryTypes = labels(WalletEntryType);
const fulfillmentStatuses = labels(FulfillmentStatus);
const paymentStatuses = labels(PaymentStatus);
const binaryVolumeEntryTypes = labels(BinaryVolumeEntryType);

export const commissionTypeLabel = (value: number) =>
  commissionTypes[value] ?? `Unknown (${value})`;
export const walletEntryTypeLabel = (value: number) =>
  walletEntryTypes[value] ?? `Unknown (${value})`;
export const fulfillmentStatusLabel = (value: number) =>
  fulfillmentStatuses[value] ?? `Unknown (${value})`;
export const paymentStatusLabel = (value: number) =>
  paymentStatuses[value] ?? `Unknown (${value})`;
export const binaryVolumeEntryTypeLabel = (value: number) =>
  binaryVolumeEntryTypes[value] ?? `Unknown (${value})`;

function badge(definition: StatusDefinition) {
  return <StatusBadge label={definition.label} tone={definition.tone} />;
}

export const AgentStatusBadge = ({ value }: { value: number }) =>
  badge(agentStatuses[value] ?? unknown(value));
export const PayoutStatusBadge = ({ value }: { value: number }) =>
  badge(payoutStatuses[value] ?? unknown(value));
export const PayoutAccountStatusBadge = ({ value }: { value: number }) =>
  badge(accountStatuses[value] ?? unknown(value));
export const OrderStatusBadge = ({ value }: { value: number }) =>
  badge(orderStatuses[value] ?? unknown(value));
export const CommissionStatusBadge = ({ value }: { value: number }) =>
  badge(commissionStatuses[value] ?? unknown(value));

export const agentStatusLabel = (value: number) =>
  (agentStatuses[value] ?? unknown(value)).label;
