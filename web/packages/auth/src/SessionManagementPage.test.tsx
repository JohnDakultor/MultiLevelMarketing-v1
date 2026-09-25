import "@modular-mlm/design-system/test-dom";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { ApiClient } from "@modular-mlm/api-client";
import { ConfirmationProvider } from "@modular-mlm/design-system";
import { cleanup, render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AuthenticationProvider } from "./AuthenticationProvider";
import { AuthenticationService } from "./AuthenticationService";
import { SessionManagementPage } from "./SessionManagementPage";

afterEach(cleanup);

function renderSessions(fetchImplementation: typeof fetch) {
  const service = new AuthenticationService(
    new ApiClient({ fetchImplementation }),
  );
  return render(
    <ConfirmationProvider>
      <AuthenticationProvider service={service}>
        <SessionManagementPage signInPath="/sign-in" />
      </AuthenticationProvider>
    </ConfirmationProvider>,
  );
}

test("session management renders, confirms, and revokes a remote session", async () => {
  const requests: Array<{ url: string; method?: string }> = [];
  let revoked = false;
  const view = renderSessions(async (input, init) => {
    const url = String(input);
    requests.push({ url, method: init?.method });
    if (url.endsWith("/api/me"))
      return Response.json({
        userId: "user-1",
        email: "admin@example.test",
        displayName: "Administrator",
        roles: ["Administrator"],
        organizationId: "org-1",
        customerId: null,
        agentId: null,
        emailVerified: true,
        mfaEnabled: false,
      });
    if (url.endsWith("/api/me/sessions"))
      return Response.json(
        revoked
          ? []
          : [
              {
                id: "session-2",
                createdAt: "2026-09-01T00:00:00Z",
                lastRotatedAt: "2026-09-01T00:00:00Z",
                expiresAt: "2026-10-01T00:00:00Z",
                revokedAt: null,
                revocationReason: null,
                isCurrent: false,
              },
            ],
      );
    if (url.endsWith("/api/security/antiforgery-token"))
      return Response.json({ requestToken: "csrf", headerName: "X-CSRF" });
    if (url.endsWith("/api/me/session/revoke")) {
      revoked = true;
      return new Response(null, { status: 204 });
    }
    throw new Error(`Unexpected request: ${url}`);
  });

  assert.ok(await view.findByRole("heading", { name: "Signed-in browser" }));
  await userEvent.click(view.getByRole("button", { name: "Revoke" }));
  const dialog = await view.findByRole("dialog", { name: "Revoke session?" });
  await userEvent.click(
    dialog.querySelector<HTMLButtonElement>("button.ds-button--danger")!,
  );
  assert.ok(await view.findByRole("heading", { name: "No sessions found" }));
  assert.ok(
    requests.some(
      (request) =>
        request.url === "/api/me/session/revoke" && request.method === "POST",
    ),
  );
});

test("session management distinguishes an API failure from an empty response", async () => {
  const view = renderSessions(async (input) => {
    const url = String(input);
    if (url.endsWith("/api/me")) return new Response(null, { status: 401 });
    if (url.endsWith("/api/me/sessions"))
      return Response.json(
        { title: "Service unavailable", status: 503 },
        { status: 503 },
      );
    throw new Error(`Unexpected request: ${url}`);
  });
  const alert = await view.findByRole("alert");
  assert.match(alert.textContent ?? "", /Session operation failed/);
  assert.ok(view.getByRole("button", { name: "Try again" }));
  assert.equal(view.queryByText("No sessions found"), null);
});
