import assert from "node:assert/strict";
import test from "node:test";
import { Roles, type CurrentUserDto } from "@modular-mlm/contracts";
import { agentNavigation } from "./agentNavigation";

function identity(roles: string[], agentId: string | null): CurrentUserDto {
  return {
    userId: "user-1",
    email: "user@example.test",
    displayName: "User",
    roles,
    organizationId: "organization-1",
    customerId: null,
    agentId,
    emailVerified: true,
    mfaEnabled: false,
  };
}

test("Agent navigation contains no Administrator operations", () => {
  const links = agentNavigation(identity([Roles.agent], "agent-1")).map(
    (item) => item.href,
  );
  assert.ok(links.includes("/network"));
  assert.ok(links.includes("/wallet"));
  assert.equal(links.includes("/administrators"), false);
  assert.equal(links.includes("/operations"), false);
});

test("identity without an Agent cannot receive Agent navigation", () => {
  assert.deepEqual(agentNavigation(identity([Roles.administrator], null)), []);
});
