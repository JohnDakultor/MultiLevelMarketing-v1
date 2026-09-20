import { strict as assert } from "node:assert";
import test from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { adminApi } from "./adminApi";

test("inventory adjustment carries the caller idempotency key and expected version", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return Response.json("adjustment-id");
    },
  });
  await adminApi.adjustInventory(
    api,
    "organization-id",
    "variant-id",
    {
      quantityDelta: 4,
      adjustmentType: 0,
      reason: "Cycle count",
      expectedVersion: 7,
    },
    "operation-key",
  );
  const request = requests.at(-1)!;
  assert.equal(
    request.url,
    "/api/organizations/organization-id/admin/inventory/variant-id/adjustments",
  );
  assert.equal(
    new Headers(request.init?.headers).get("Idempotency-Key"),
    "operation-key",
  );
  assert.equal(
    (JSON.parse(String(request.init?.body)) as { expectedVersion: number })
      .expectedVersion,
    7,
  );
});

test("Agent review operations use the tenant-scoped lifecycle endpoint", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return new Response(null, { status: 204 });
    },
  });

  await adminApi.agentAction(api, "org-1", "agent-1", "reject");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/agents/agent-1/reject",
  );
  assert.equal(requests.at(-1)?.init?.method, "POST");
});

test("Admin order discovery preserves tenant scope and filters", async () => {
  const requests: string[] = [];
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      requests.push(String(input));
      return Response.json({ items: [], page: 2, pageSize: 20, totalCount: 0 });
    },
  });
  const parameters = new URLSearchParams({
    page: "2",
    pageSize: "20",
    search: "ORD-42",
    paymentStatus: "2",
  });
  await adminApi.orders(api, "org-1", parameters);
  assert.equal(
    requests.at(-1),
    "/api/organizations/org-1/admin/orders?page=2&pageSize=20&search=ORD-42&paymentStatus=2",
  );
});

test("placement, pairing, and payout account actions use selected resources", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return Response.json("result");
    },
  });
  await adminApi.placeAgent(api, "org-1", "agent-1", "parent-1", 0);
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/agents/agent-1/placement",
  );
  assert.deepEqual(JSON.parse(String(requests.at(-1)?.init?.body)), {
    parentAgentId: "parent-1",
    side: 0,
  });
  await adminApi.processPairing(api, "org-1", "agent-1", {
    commissionPlanId: "plan-1",
    periodStart: "2026-01-01T00:00:00Z",
    periodEnd: "2026-02-01T00:00:00Z",
  });
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/agents/agent-1/pairing/runs",
  );
  await adminApi.payoutAccountAction(api, "org-1", "account-1", "verify");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/payouts/accounts/account-1/verify",
  );
});

test("placement discovery and financial operations use selected tenant resources", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      if (init?.method === "GET" || !init?.method)
        return Response.json({
          items: [],
          page: 1,
          pageSize: 100,
          totalCount: 0,
        });
      return new Response(null, { status: 204 });
    },
  });

  await adminApi.placementCandidates(api, "org-1");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/agents?page=1&pageSize=100&search=",
  );

  await adminApi.requestPaymentRefund(api, "org-1", "order-1", 250, "Damaged");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/orders/order-1/refunds",
  );
  assert.deepEqual(JSON.parse(String(requests.at(-1)?.init?.body)), {
    amount: 250,
    reason: "Damaged",
  });

  await adminApi.reconcilePayment(api, "org-1", "payment-1");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/orders/payments/payment-1/reconcile",
  );
});
