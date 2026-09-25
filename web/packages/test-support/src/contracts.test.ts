import assert from "node:assert/strict";
import test from "node:test";
import type {
  AdminDashboardDto,
  CartDto,
  CurrentUserDto,
  OrderDetailsDto,
  PublicOrganizationConfigDto,
  WalletSummaryDto,
} from "@modular-mlm/contracts";
import {
  adminDashboardFixture,
  cartFixture,
  currentUserFixture,
  orderDetailsFixture,
  organizationFixture,
  walletSummaryFixture,
} from "./index";

test("current-user fixture matches the backend identity response contract", () => {
  const fixture: CurrentUserDto = currentUserFixture();
  const roundTrip = JSON.parse(JSON.stringify(fixture)) as CurrentUserDto;
  assert.equal(roundTrip.emailVerified, true);
  assert.equal(roundTrip.organizationId, null);
  assert.deepEqual(roundTrip.roles, []);
});

test("public organization fixture contains the tenant bootstrap contract", () => {
  const fixture: PublicOrganizationConfigDto = organizationFixture();
  assert.equal(fixture.currencyCode.length, 3);
  assert.match(fixture.primaryColor, /^#[0-9a-f]{6}$/i);
  assert.equal(typeof fixture.agentProgramEnabled, "boolean");
});

test("commerce, Agent, and Administrator fixtures survive JSON deserialization", () => {
  const cart = JSON.parse(JSON.stringify(cartFixture())) as CartDto;
  const order = JSON.parse(
    JSON.stringify(orderDetailsFixture()),
  ) as OrderDetailsDto;
  const wallet = JSON.parse(
    JSON.stringify(walletSummaryFixture()),
  ) as WalletSummaryDto;
  const dashboard = JSON.parse(
    JSON.stringify(adminDashboardFixture()),
  ) as AdminDashboardDto;

  assert.equal(cart.currency, "PHP");
  assert.equal(order.shippingAddress.countryCode, "PH");
  assert.equal(wallet.available, 0);
  assert.deepEqual(dashboard.tasks, []);
});
