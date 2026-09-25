import assert from "node:assert/strict";
import test from "node:test";
import { ApiError } from "@modular-mlm/api-client";
import { safeReturnPath, signInErrorMessages } from "./SignInForm";

test("accepts only local return paths", () => {
  assert.equal(safeReturnPath("/wallet", "/"), "/wallet");
  assert.equal(safeReturnPath("https://attacker.example", "/"), "/");
  assert.equal(safeReturnPath("//attacker.example", "/"), "/");
  assert.equal(safeReturnPath(null, "/"), "/");
});

test("sign-in errors explain credential, permission, throttling, and connectivity failures", () => {
  assert.deepEqual(
    signInErrorMessages(new ApiError(401, { title: "Unauthorized" })),
    [
      "The email or password is incorrect. Check your credentials and try again.",
    ],
  );
  assert.match(
    signInErrorMessages(new ApiError(403, { title: "Forbidden" }))[0]!,
    /not allowed to sign in here/i,
  );
  assert.match(signInErrorMessages(new ApiError(429, {}))[0]!, /too many/i);
  assert.match(
    signInErrorMessages(new ApiError(0, {}))[0]!,
    /could not be reached/i,
  );
});
