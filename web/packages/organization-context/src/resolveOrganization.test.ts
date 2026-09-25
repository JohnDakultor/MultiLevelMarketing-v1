import assert from "node:assert/strict";
import test from "node:test";
import {
  hasDevelopmentSlug,
  isDevelopmentHost,
  normalizeHostName,
} from "./resolveOrganization";

test("normalizes a hostname without leaking its port into tenant resolution", () => {
  assert.equal(normalizeHostName("GreenZero.Example:443"), "greenzero.example");
});

test("allows slug fallback only for loopback development hosts", () => {
  assert.equal(isDevelopmentHost("webapi.dev.localhost"), true);
  assert.equal(isDevelopmentHost("127.0.0.1:3000"), true);
  assert.equal(isDevelopmentHost("greenzero.example"), false);
});

test("uses the configured slug directly on localhost", () => {
  assert.equal(
    hasDevelopmentSlug({
      hostName: "localhost:3000",
      developmentSlug: "greenzero",
    }),
    true,
  );
  assert.equal(
    hasDevelopmentSlug({
      hostName: "greenzero.example",
      developmentSlug: "greenzero",
    }),
    false,
  );
});
