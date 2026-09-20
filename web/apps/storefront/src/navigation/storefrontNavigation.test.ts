import assert from "node:assert/strict";
import test from "node:test";
import type { CurrentUserDto } from "@modular-mlm/contracts";
import { storefrontNavigation } from "./storefrontNavigation";

test("anonymous storefront navigation does not expose customer account", () => {
  assert.equal(
    storefrontNavigation(null).some((item) => item.href === "/account"),
    false,
  );
});

test("authenticated storefront navigation exposes customer account", () => {
  const user: CurrentUserDto = {
    userId: "user-1",
    email: "customer@example.test",
    displayName: "Customer",
    roles: [],
    organizationId: null,
    customerId: "customer-1",
    agentId: null,
    emailVerified: true,
    mfaEnabled: false,
  };
  assert.equal(
    storefrontNavigation(user).some((item) => item.href === "/account"),
    true,
  );
});
