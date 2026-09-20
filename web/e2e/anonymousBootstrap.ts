import type { Page } from "@playwright/test";
import { organizationFixture } from "@modular-mlm/test-support";

/** Contract-valid bootstrap for UI-only accessibility and responsive checks. */
export async function installAnonymousBootstrap(page: Page): Promise<void> {
  await page.route("**/api/me", async (route) => {
    await route.fulfill({ status: 204 });
  });
  await page.route("**/api/organizations/**/public-config", async (route) => {
    await route.fulfill({ json: organizationFixture() });
  });
  await page.route("**/api/organizations/public-config", async (route) => {
    await route.fulfill({ json: organizationFixture() });
  });
}
