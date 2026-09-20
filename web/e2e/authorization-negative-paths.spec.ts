import { expect, test } from "@playwright/test";
import { requireRealStack, requiredEnvironment, signIn } from "./realStack";

test("Customer cannot open the Agent workspace", async ({ page }, testInfo) => {
  requireRealStack(testInfo, "agent-portal");
  await signIn(page, "E2E_CUSTOMER_EMAIL", "E2E_CUSTOMER_PASSWORD");
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Access denied" }),
  ).toBeVisible();
});

test("Customer and Agent cannot open the Administrator workspace", async ({
  browser,
}, testInfo) => {
  requireRealStack(testInfo, "admin-portal");
  for (const role of ["CUSTOMER", "AGENT"] as const) {
    const context = await browser.newContext({
      baseURL: testInfo.project.use.baseURL,
    });
    const page = await context.newPage();
    await signIn(page, `E2E_${role}_EMAIL`, `E2E_${role}_PASSWORD`);
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: "Access denied" }),
    ).toBeVisible();
    await context.close();
  }
});

test("Administrator cannot access another Organization and 401 differs from 403", async ({
  page,
  request,
}, testInfo) => {
  requireRealStack(testInfo, "admin-portal");
  const foreignOrganizationId = requiredEnvironment(
    "E2E_FOREIGN_ORGANIZATION_ID",
  );
  const anonymous = await request.get(
    `/api/organizations/${foreignOrganizationId}/admin/settings`,
  );
  expect(anonymous.status()).toBe(401);

  await signIn(page, "E2E_ADMIN_EMAIL", "E2E_ADMIN_PASSWORD");
  const forbidden = await page.request.get(
    `/api/organizations/${foreignOrganizationId}/admin/settings`,
  );
  expect(forbidden.status()).toBe(403);
});
