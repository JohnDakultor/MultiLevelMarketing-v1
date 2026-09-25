import assert from "node:assert/strict";
import test from "node:test";
import {
  ApiError,
  firstFieldError,
  isRetrySafe,
  validationFieldErrors,
  validationMessages,
} from "./ApiError";

test("ApiError preserves Problem Details validation messages", () => {
  const error = new ApiError(422, {
    title: "Validation failed",
    errors: {
      email: ["Email is required."],
      password: ["Password is too short."],
    },
  });

  assert.equal(error.status, 422);
  assert.deepEqual(validationMessages(error), [
    "Email is required.",
    "Password is too short.",
  ]);
});

test("validation keys map from backend property paths to form field names", () => {
  const errors = validationFieldErrors(
    new ApiError(422, {
      errors: {
        "Request.Email": ["Enter a valid email."],
        "Addresses[0].PostalCode": ["Postal code is required."],
      },
    }),
  );
  assert.equal(firstFieldError(errors, "email"), "Enter a valid email.");
  assert.equal(
    firstFieldError(errors, "postalCode"),
    "Postal code is required.",
  );
});

test("retry guidance excludes permission, validation, and conflict failures", () => {
  assert.equal(isRetrySafe(new ApiError(403, {})), false);
  assert.equal(isRetrySafe(new ApiError(422, {})), false);
  assert.equal(isRetrySafe(new ApiError(409, {})), false);
  assert.equal(isRetrySafe(new ApiError(503, {})), true);
  assert.equal(isRetrySafe(new ApiError(0, {})), true);
});

test("ApiError distinguishes unauthorized and forbidden responses", () => {
  assert.equal(new ApiError(401, {}).isUnauthorized, true);
  assert.equal(new ApiError(403, {}).isForbidden, true);
  assert.equal(new ApiError(409, {}).isConflict, true);
});

test("generic HTTP titles are replaced with actionable status messages", () => {
  assert.equal(
    new ApiError(401, { title: "Unauthorized" }).message,
    "Your session has expired. Sign in again to continue.",
  );
  assert.equal(
    new ApiError(503, { title: "Service Unavailable" }).message,
    "The service is temporarily unavailable. Try again shortly.",
  );
  assert.equal(
    new ApiError(409, { detail: "The payout was already processed." }).message,
    "The payout was already processed.",
  );
});
