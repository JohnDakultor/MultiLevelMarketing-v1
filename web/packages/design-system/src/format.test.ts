import assert from "node:assert/strict";
import test from "node:test";
import {
  formatDate,
  formatDateTime,
  formatMoney,
  formatStatusLabel,
} from "./format";

test("money formatting honors currency and locale", () => {
  assert.match(formatMoney(1234.5, "PHP", "en-PH"), /1,234\.50/);
});

test("date formatting handles missing and invalid values", () => {
  assert.equal(formatDate(null, "en-US"), "—");
  assert.equal(formatDateTime("not-a-date", "en-US"), "—");
  assert.match(formatDate("2026-09-19T00:00:00Z", "en-US"), /Sep 19, 2026/);
});

test("status labels are readable without relying on badge color", () => {
  assert.equal(formatStatusLabel("PendingApproval"), "Pending Approval");
  assert.equal(formatStatusLabel("payment_failed"), "Payment failed");
});
