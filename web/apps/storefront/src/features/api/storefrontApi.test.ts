import { strict as assert } from "node:assert";
import test from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { storefrontApi } from "./storefrontApi";

test("checkout sends address snapshots without client-owned price or identity fields", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return Response.json("order-id");
    },
  });
  const address = {
    recipientName: "Customer",
    phoneNumber: "09170000000",
    addressLine1: "1 Main St",
    addressLine2: null,
    barangay: null,
    cityOrMunicipality: "Manila",
    province: "Metro Manila",
    postalCode: "1000",
    countryCode: "PH",
  };
  await storefrontApi.checkout(api, "organization-id", address, address);
  const body = JSON.parse(String(requests.at(-1)?.init?.body)) as Record<
    string,
    unknown
  >;
  assert.deepEqual(Object.keys(body).sort(), [
    "billingAddress",
    "shippingAddress",
  ]);
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/organization-id/orders/checkout",
  );
});

test("customer order mutations use owned-order routes and minimal payloads", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      requests.push({ url: String(input), init });
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      return new Response(null, { status: 204 });
    },
  });

  await storefrontApi.cancelOrder(api, "org-1", "order-1", "Duplicate order");
  assert.equal(
    requests.at(-1)?.url,
    "/api/organizations/org-1/me/orders/order-1/cancellation",
  );
  assert.equal(requests.at(-1)?.init?.method, "POST");
  assert.deepEqual(JSON.parse(String(requests.at(-1)?.init?.body)), {
    reason: "Duplicate order",
  });
});

test("account security uses the ASP.NET Identity management contracts", async () => {
  const requests: Array<{ url: string; init?: RequestInit }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      const url = String(input);
      requests.push({ url, init });
      if (url.endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      if (init?.method === "POST")
        return Response.json({
          sharedKey: "key",
          recoveryCodesLeft: 0,
          recoveryCodes: null,
          isTwoFactorEnabled: false,
          isMachineRemembered: false,
        });
      return Response.json({
        email: "customer@example.test",
        isEmailConfirmed: true,
      });
    },
  });

  await storefrontApi.identityInfo(api);
  await storefrontApi.updateIdentityInfo(api, {
    newEmail: "new@example.test",
    oldPassword: "old-password",
    newPassword: "new-password",
  });
  await storefrontApi.updateTwoFactor(api, { resetSharedKey: true });

  assert.equal(requests[0]?.url, "/api/Users/manage/info");
  assert.equal(requests[2]?.url, "/api/Users/manage/info");
  assert.equal(requests[2]?.init?.method, "POST");
  assert.equal(requests.at(-1)?.url, "/api/Users/manage/2fa");
  assert.deepEqual(JSON.parse(String(requests.at(-1)?.init?.body)), {
    resetSharedKey: true,
  });
});

test("order history sends the backend-supported status filter", async () => {
  const requests: string[] = [];
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      requests.push(String(input));
      return Response.json({
        items: [],
        page: 2,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: true,
        hasNextPage: false,
      });
    },
  });

  await storefrontApi.orders(api, "org-1", 2, 3);

  assert.equal(
    requests[0],
    "/api/organizations/org-1/me/orders?page=2&pageSize=20&status=3",
  );
});

test("catalog discovery sends server-side search filters sorting and pagination", async () => {
  const requests: string[] = [];
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      requests.push(String(input));
      return Response.json({
        items: [],
        page: 3,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: true,
        hasNextPage: false,
      });
    },
  });

  await storefrontApi.products(api, "org-1", {
    page: 3,
    search: "green tea",
    categoryId: "category-1",
    minimumPrice: 100,
    maximumPrice: 500,
    availability: "InStock",
    sort: "PriceAscending",
  });

  const [path, query] = requests[0]!.split("?");
  assert.equal(path, "/api/organizations/org-1/products");
  assert.deepEqual(Object.fromEntries(new URLSearchParams(query)), {
    page: "3",
    pageSize: "24",
    availability: "InStock",
    sort: "PriceAscending",
    search: "green tea",
    categoryId: "category-1",
    minimumPrice: "100",
    maximumPrice: "500",
  });
});
