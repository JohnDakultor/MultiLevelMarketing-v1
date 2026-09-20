import assert from "node:assert/strict";
import test from "node:test";
import { Roles, type CurrentUserDto } from "@modular-mlm/contracts";
import { adminNavigation } from "./adminNavigation";

function identity(roles: string[]): CurrentUserDto {
  return {
    userId: "user-1",
    email: "user@example.test",
    displayName: "User",
    roles,
    organizationId: "organization-1",
    customerId: null,
    agentId: null,
    emailVerified: true,
    mfaEnabled: false,
  };
}

test("Administrator navigation is generated from administration capability", () => {
  const links = adminNavigation(identity([Roles.administrator])).map(
    (item) => item.href,
  );
  assert.ok(links.includes("/catalog"));
  assert.ok(links.includes("/audit"));
  assert.ok(links.includes("/operations"));
});

test("Platform Administrator receives organization provisioning navigation", () => {
  const links = adminNavigation(identity([Roles.platformAdministrator]));
  assert.equal(
    links.some((item) => item.href === "/platform/organizations/new"),
    true,
  );
});

test("Agent identity does not receive Administrator navigation", () => {
  assert.deepEqual(adminNavigation(identity([Roles.agent])), []);
});
