import assert from "node:assert/strict";
import test from "node:test";
import { sanitizeTelemetryEvent } from "./index";

test("telemetry removes credential and personal-data attributes", () => {
  const event = sanitizeTelemetryEvent({
    name: "request.failed",
    outcome: "failure",
    attributes: {
      routeName: "checkout",
      authorization: "Bearer secret",
      customerEmail: "person@example.test",
      paymentReference: "private-reference",
    },
  });

  assert.deepEqual(event.attributes, { routeName: "checkout" });
});
