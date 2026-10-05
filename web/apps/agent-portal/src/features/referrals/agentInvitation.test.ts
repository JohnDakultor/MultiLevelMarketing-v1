import assert from "node:assert/strict";
import test from "node:test";
import {
  createAgentInvitationUrl,
  createAgentOnboardingPath,
} from "./agentInvitation";

test("creates an Agent Portal invitation URL with a normalized sponsor code", () => {
  assert.equal(
    createAgentInvitationUrl("https://agents.example.com", " ref-123 "),
    "https://agents.example.com/register?sponsor=REF-123",
  );
});

test("preserves a sponsor code in the post-authentication onboarding path", () => {
  assert.equal(createAgentOnboardingPath("REF 123"), "/?sponsor=REF%20123");
  assert.equal(createAgentOnboardingPath(null), "/");
});
