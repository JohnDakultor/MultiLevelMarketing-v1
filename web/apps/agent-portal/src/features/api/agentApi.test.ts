import { strict as assert } from "node:assert";
import test from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { agentApi } from "./agentApi";

test("current referral link uses the current-Agent endpoint without a supplied Agent id", async () => {
  let requestedUrl = "";
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      requestedUrl = String(input);
      return Response.json({
        referralCode: "CODE",
        productId: null,
        relativeUrl: "/r/CODE",
      });
    },
  });
  await agentApi.referral(api, "organization-id");
  assert.equal(
    requestedUrl,
    "/api/organizations/organization-id/agent/referral",
  );
});

test("payout requests remain scoped to the authenticated Agent route", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return Response.json("payout-id");
    },
  });

  await agentApi.requestPayout(
    api,
    "org-1",
    "agent-1",
    "account-1",
    500,
    "PHP",
  );
  const request = requests.at(-1)!;
  assert.equal(
    request.url,
    "/api/organizations/org-1/agents/agent-1/finance/payouts",
  );
  assert.deepEqual(JSON.parse(String(request.init?.body)), {
    payoutAccountId: "account-1",
    amount: 500,
    currency: "PHP",
  });
});

test("Step 2 read operations match the implemented Agent endpoint routes", async () => {
  const urls: string[] = [];
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      urls.push(String(input));
      return Response.json({ items: [], page: 1, pageSize: 20, totalCount: 0 });
    },
  });

  await agentApi.children(api, "org-1", "agent-1");
  await agentApi.ancestors(api, "org-1", "agent-1");
  await agentApi.saleDetails(api, "org-1", "order-1");
  await agentApi.productSales(api, "org-1", 2);
  await agentApi.commission(api, "org-1", "agent-1", "commission-1");
  await agentApi.payout(api, "org-1", "agent-1", "payout-1");
  await agentApi.referralDashboard(api, "org-1", "agent-1");

  assert.deepEqual(urls, [
    "/api/organizations/org-1/agents/agent-1/network/children",
    "/api/organizations/org-1/agents/agent-1/network/ancestors",
    "/api/organizations/org-1/agent/sales/order-1",
    "/api/organizations/org-1/agent/sales/products?page=2&pageSize=20",
    "/api/organizations/org-1/agents/agent-1/finance/commissions/commission-1",
    "/api/organizations/org-1/agents/agent-1/payouts/payout-1",
    "/api/organizations/org-1/referrals/agents/agent-1/dashboard",
  ]);
});

test("referral-code regeneration uses an antiforgery-protected POST", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return Response.json("NEWCODE");
    },
  });

  const code = await agentApi.regenerateReferralCode(api, "org-1", "agent-1");

  assert.equal(code, "NEWCODE");
  const request = requests.at(-1)!;
  assert.equal(
    request.url,
    "/api/organizations/org-1/referrals/agents/agent-1/code",
  );
  assert.equal(request.init?.method, "POST");
});
