import assert from "node:assert/strict";
import test from "node:test";
import { queryKey, queryKeys } from "./queryKeys";

test("query keys always include tenant scope before resource identity", () => {
  assert.deepEqual(queryKeys.order("org-1", "order-2"), [
    "organization",
    "org-1",
    "order",
    "order-2",
  ]);
  assert.deepEqual(queryKey(null, "catalog", 1), [
    "organization",
    "unresolved",
    "catalog",
    1,
  ]);
});
