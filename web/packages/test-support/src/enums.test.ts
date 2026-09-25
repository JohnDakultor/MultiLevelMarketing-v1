import assert from "node:assert/strict";
import test from "node:test";
import {
  AgentStatus,
  FulfillmentStatus,
  OrderStatus,
  PaymentStatus,
  PayoutStatus,
  ProductStatus,
} from "@modular-mlm/contracts";

test("numeric status contracts match the backend domain enum order", () => {
  assert.deepEqual(Object.values(AgentStatus), [0, 1, 2, 3, 4, 5]);
  assert.deepEqual(Object.values(ProductStatus), [0, 1, 2]);
  assert.deepEqual(Object.values(OrderStatus), [0, 1, 2, 3, 4, 5, 6, 7]);
  assert.deepEqual(Object.values(PaymentStatus), [0, 1, 2, 3, 4, 5]);
  assert.deepEqual(Object.values(FulfillmentStatus), [0, 1, 2, 3, 4, 5]);
  assert.deepEqual(Object.values(PayoutStatus), [0, 1, 2, 3, 4, 5, 6, 7]);
});
