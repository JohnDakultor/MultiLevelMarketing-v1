import assert from "node:assert/strict";
import test from "node:test";
import {
  AgentStatus,
  CommissionPlanStatus,
  OrderItemRefundStatus,
  OrderStatus,
  PaymentRefundStatus,
  PayoutStatus,
  ProductStatus,
  WalletStatus,
} from "@modular-mlm/contracts";
import {
  agentStatus,
  commissionPlanStatus,
  orderItemRefundStatus,
  orderStatus,
  paymentRefundStatus,
  payoutStatus,
  productStatus,
  walletStatus,
} from "./status";

test("Admin status presentations mirror backend lifecycle enums", () => {
  assert.deepEqual(agentStatus(AgentStatus.pendingApproval), {
    label: "Pending approval",
    tone: "warning",
  });
  assert.equal(productStatus(ProductStatus.active).label, "Published");
  assert.equal(orderStatus(OrderStatus.delivered).label, "Delivered");
  assert.equal(payoutStatus(PayoutStatus.processing).label, "Processing");
  assert.equal(
    commissionPlanStatus(CommissionPlanStatus.retired).label,
    "Retired",
  );
  assert.equal(walletStatus(WalletStatus.held).label, "Held");
  assert.equal(
    orderItemRefundStatus(OrderItemRefundStatus.reversalFailed).label,
    "Reversal failed",
  );
  assert.equal(
    paymentRefundStatus(PaymentRefundStatus.succeeded).label,
    "Succeeded",
  );
});

test("Unknown backend status values remain visible for support diagnostics", () => {
  assert.equal(agentStatus(999).label, "Unknown (999)");
  assert.equal(orderStatus(999).tone, "neutral");
});
