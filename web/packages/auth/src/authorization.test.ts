import assert from "node:assert/strict";
import test from "node:test";
import { Roles, type CurrentUserDto } from "@modular-mlm/contracts";
import {
  Capabilities,
  canAccessPortal,
  canAccessAgentOrganization,
  canAdministerOrganization,
  hasCapability,
  preferredPortal,
} from "./authorization";

const organizationId = "00000000-0000-0000-0000-000000000010";

function user(overrides: Partial<CurrentUserDto> = {}): CurrentUserDto {
  return {
    userId: "user-1",
    email: "user@example.test",
    displayName: "Test User",
    roles: [],
    organizationId: null,
    customerId: null,
    agentId: null,
    emailVerified: true,
    mfaEnabled: false,
    ...overrides,
  };
}

test("Agent portal requires both the Agent role and resolved Agent identity", () => {
  assert.equal(canAccessPortal(user({ roles: [Roles.agent] }), "agent"), false);
  assert.equal(
    canAccessPortal(
      user({ roles: [Roles.agent], agentId: "agent-1" }),
      "agent",
    ),
    true,
  );
});

test("Administrator access remains scoped to the current Organization", () => {
  const administrator = user({ roles: [Roles.administrator], organizationId });
  assert.equal(canAdministerOrganization(administrator, organizationId), true);
  assert.equal(
    canAdministerOrganization(
      administrator,
      "00000000-0000-0000-0000-000000000099",
    ),
    false,
  );
  assert.equal(preferredPortal(administrator), "admin");
});

test("navigation capabilities are derived from identity instead of UI role strings", () => {
  const agent = user({
    roles: [Roles.agent],
    organizationId,
    agentId: "agent-1",
  });

  assert.equal(hasCapability(agent, Capabilities.agentWorkspace), true);
  assert.equal(
    hasCapability(agent, Capabilities.organizationAdministration),
    false,
  );
  assert.equal(canAccessAgentOrganization(agent, organizationId), true);
  assert.equal(
    canAccessAgentOrganization(agent, "another-organization"),
    false,
  );
});
