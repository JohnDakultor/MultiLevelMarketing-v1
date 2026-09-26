import { strict as assert } from "node:assert";
import test from "node:test";
import { ApiClient } from "./ApiClient";

test("optional antiforgery allows an anonymous mutation after token lookup returns 401", async () => {
  const requests: string[] = [];
  const client = new ApiClient({
    fetchImplementation: async (input) => {
      const url = String(input);
      requests.push(url);
      if (url.endsWith("/api/security/antiforgery-token"))
        return new Response(null, { status: 401 });
      return Response.json("cart-id", { status: 201 });
    },
  });

  const result = await client.request<string>("/api/cart/items", {
    method: "POST",
    anonymous: true,
    optionalAntiforgery: true,
    body: { productVariantId: "variant", quantity: 1 },
  });

  assert.equal(result, "cart-id");
  assert.deepEqual(requests, [
    "/api/security/antiforgery-token",
    "/api/cart/items",
  ]);
});

test("multipart requests preserve the browser-generated boundary", async () => {
  let request: RequestInit | undefined;
  const client = new ApiClient({
    authentication: "bearer",
    fetchImplementation: async (_input, init) => {
      request = init;
      return Response.json({ url: "https://cdn.example/logo.png" });
    },
  });
  const form = new FormData();
  form.append("file", new Blob(["logo"], { type: "image/png" }), "logo.png");
  await client.request("/api/assets", { method: "POST", body: form });
  assert.equal(request?.body, form);
  assert.equal(new Headers(request?.headers).has("Content-Type"), false);
});

test("every API operation carries a client correlation identifier", async () => {
  let request: RequestInit | undefined;
  const client = new ApiClient({
    authentication: "bearer",
    createCorrelationId: () => "correlation-123",
    fetchImplementation: async (_input, init) => {
      request = init;
      return Response.json({ ok: true });
    },
  });

  await client.request("/api/example");

  assert.equal(
    new Headers(request?.headers).get("X-Correlation-ID"),
    "correlation-123",
  );
});

test("every API operation and antiforgery lookup requests API version 1.0", async () => {
  const headers: Headers[] = [];
  const client = new ApiClient({
    fetchImplementation: async (input, init) => {
      headers.push(new Headers(init?.headers));
      if (String(input).endsWith("/api/security/antiforgery-token"))
        return Response.json({
          headerName: "X-XSRF-TOKEN",
          requestToken: "token",
        });
      return new Response(null, { status: 204 });
    },
  });

  await client.request<void>("/api/example", { method: "POST", body: {} });

  assert.deepEqual(
    headers.map((value) => value.get("Api-Version")),
    ["1.0", "1.0"],
  );
});

test("successful HTTP 200 responses with an empty body resolve as void", async () => {
  const client = new ApiClient({
    authentication: "bearer",
    fetchImplementation: async () => new Response(null, { status: 200 }),
  });

  await assert.doesNotReject(() =>
    client.request<void>("/api/Users/logout", {
      method: "POST",
      body: {},
    }),
  );
});
