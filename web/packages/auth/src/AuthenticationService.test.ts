import assert from "node:assert/strict";
import test from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { AuthenticationService } from "./AuthenticationService";

test("session management lists and revokes the selected authenticated session", async () => {
  const requests: Array<{ url: string; method?: string }> = [];
  const api = new ApiClient({
    fetchImplementation: async (input, init) => {
      const url = String(input);
      requests.push({ url, method: init?.method });
      if (url.endsWith("/api/security/antiforgery-token"))
        return Response.json({ requestToken: "token", headerName: "X-CSRF" });
      if (url.endsWith("/api/me/sessions"))
        return Response.json([
          {
            id: "session-1",
            createdAt: "2026-01-01T00:00:00Z",
            lastRotatedAt: "2026-01-01T00:00:00Z",
            expiresAt: "2026-02-01T00:00:00Z",
            revokedAt: null,
            revocationReason: null,
            isCurrent: true,
          },
        ]);
      return new Response(null, { status: 204 });
    },
  });
  const authentication = new AuthenticationService(api);

  const sessions = await authentication.getSessions();
  await authentication.revokeCurrentSession(sessions[0]!.id);

  assert.equal(sessions[0]?.isCurrent, true);
  assert.deepEqual(
    requests.map((request) => request.url),
    [
      "/api/me/sessions",
      "/api/security/antiforgery-token",
      "/api/me/session/revoke",
    ],
  );
  assert.equal(requests.at(-1)?.method, "POST");
});

test("login discards an antiforgery token from the previous identity", async () => {
  const requests: string[] = [];
  let tokenCount = 0;
  const api = new ApiClient({
    fetchImplementation: async (input) => {
      const url = String(input);
      requests.push(url);
      if (url.endsWith("/api/security/antiforgery-token")) {
        tokenCount += 1;
        return Response.json({
          requestToken: `token-${tokenCount}`,
          headerName: "X-CSRF",
        });
      }
      return new Response(null, { status: 204 });
    },
  });
  const authentication = new AuthenticationService(api);

  await api.request("/api/protected", { method: "POST", body: {} });
  await authentication.signIn({
    email: "user@example.test",
    password: "Secret123!",
  });
  await authentication.signOut();

  assert.equal(tokenCount, 2);
});

test("logout treats an already expired server session as signed out", async () => {
  const api = new ApiClient({
    fetchImplementation: async (input) =>
      String(input).endsWith("/api/security/antiforgery-token")
        ? Response.json({ requestToken: "token", headerName: "X-CSRF" })
        : Response.json({ title: "Unauthorized" }, { status: 401 }),
  });

  await assert.doesNotReject(() => new AuthenticationService(api).signOut());
});
