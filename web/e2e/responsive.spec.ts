import { expect, test } from "@playwright/test";
import { installAnonymousBootstrap } from "./anonymousBootstrap";

test("entry routes do not introduce horizontal page scrolling on mobile", async ({
  page,
}) => {
  await installAnonymousBootstrap(page);
  await page.setViewportSize({ width: 360, height: 800 });
  await page.goto("/sign-in");
  const dimensions = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    content: document.documentElement.scrollWidth,
  }));
  expect(dimensions.content).toBeLessThanOrEqual(dimensions.viewport);
});
