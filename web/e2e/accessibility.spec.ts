import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import { installAnonymousBootstrap } from "./anonymousBootstrap";

test.beforeEach(async ({ page }) => {
  await installAnonymousBootstrap(page);
});

test("public entry point has no serious or critical accessibility findings", async ({
  page,
}) => {
  await page.goto("/sign-in");
  await expect(page.getByRole("heading", { name: /sign in/i })).toBeVisible();
  const results = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"])
    .analyze();
  const serious = results.violations.filter(
    (violation) =>
      violation.impact === "serious" || violation.impact === "critical",
  );
  expect(serious).toEqual([]);
});

test("sign-in workflow is keyboard operable", async ({ page }) => {
  await page.goto("/sign-in");
  await page.keyboard.press("Tab");
  expect(await page.evaluate(() => document.activeElement?.tagName)).not.toBe(
    "BODY",
  );
  await page.getByLabel("Email address").focus();
  await page.keyboard.type("keyboard@example.test");
  await page.keyboard.press("Tab");
  await page.keyboard.type("not-a-real-password");
  await expect(page.getByRole("button", { name: /sign in/i })).toBeEnabled();
});

test("protected workspace routes redirect an anonymous visitor", async ({
  page,
}, testInfo) => {
  test.skip(
    testInfo.project.name === "storefront",
    "The public storefront is intentionally anonymous.",
  );
  await page.goto("/");
  await expect(page).toHaveURL(/\/sign-in\?returnTo=/);
});
