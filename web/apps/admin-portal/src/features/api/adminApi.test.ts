import { strict as assert } from "node:assert";
import test from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { adminApi } from "./adminApi";

test("organization provisioning lookup encodes the slug for recovery", async () => {
  const requests: string[] = [];
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      requests.push(String(input));
      return Response.json({
        organizationId: "organization-id",
        name: "La De Perfum",
        slug: "la-de-perfum",
        brandingPublished: false,
      });
    },
  });

  const result = await adminApi.organizationProvisioning(api, "la-de-perfum");

  assert.equal(requests.at(-1), "/api/organizations/provisioning/la-de-perfum");
  assert.equal(result.brandingPublished, false);
});

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

test("product image operations use multipart upload and the tenant-scoped product route", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      if (init?.method === "DELETE") return new Response(null, { status: 204 });
      return Response.json({
        objectKey: "organizations/org-1/products/product-1/images/image.png",
        url: "https://assets.test/image.png",
        contentType: "image/png",
        contentLength: 3,
      });
    },
  });
  const image = new File([new Uint8Array([1, 2, 3])], "product.png", {
    type: "image/png",
  });

  await adminApi.uploadProductImage(api, "org-1", "product-1", image);
  const upload = requests.at(-1)!;
  assert.equal(
    upload.url,
    "/api/organizations/org-1/admin/products/product-1/image",
  );
  assert.equal(upload.init?.method, "POST");
  assert.ok(upload.init?.body instanceof FormData);
  assert.equal(new Headers(upload.init?.headers).has("Content-Type"), false);

  await adminApi.removeProductImage(api, "org-1", "product-1");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/products/product-1/image",
  );
  assert.equal(requests.at(-1)?.init?.method, "DELETE");
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

test("new administration workflows use tenant-scoped backend contracts", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return init?.method
        ? new Response(null, { status: 204 })
        : Response.json({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    },
  });

  await adminApi.categoryAction(api, "org-1", "category-1", "archive");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/categories/category-1/archive",
  );
  await adminApi.startOrderProcessing(api, "org-1", "order-1");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/orders/order-1/processing",
  );
  await adminApi.updatePlan(api, "org-1", "plan-1", {
    expectedConfigurationVersion: 4,
  });
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/compensation/plans/plan-1",
  );
  await adminApi.adminWallets(api, "org-1", 1, "AGENT");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/wallets?page=1&pageSize=20&search=AGENT&negativeOnly=false",
  );
  await adminApi.adminWallets(api, "org-1", 1, "");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/wallets?page=1&pageSize=20&negativeOnly=false",
  );
  await adminApi.commissionLedger(api, "org-1", 2);
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/commissions?page=2&pageSize=20&includeReversals=true",
  );
  await adminApi.payoutAccounts(api, "org-1", 1, undefined, "Pending");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/payouts/accounts?page=1&pageSize=20&status=Pending",
  );
  await adminApi.updateReferralSettings(api, "org-1", {
    attributionWindowDays: 30,
    allowReferralOverride: false,
    referralLockAfterFirstPurchase: true,
  });
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/referral-settings",
  );

  await adminApi.customers(api, "org-1", 2, "maria", "Suspended");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/customers?page=2&pageSize=20&search=maria&status=Suspended",
  );
  await adminApi.changeCustomerStatus(api, "org-1", "customer-1", 1, "Review");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/admin/customers/customer-1/status",
  );
});
