import { expect, type Page, type TestInfo } from "@playwright/test";

export const realStackEnabled = process.env.E2E_REAL_STACK === "1";

export function requireRealStack(testInfo: TestInfo, project: string): void {
  testInfo.skip(
    testInfo.project.name !== project,
    `This workflow belongs to the ${project} deployment.`,
  );
  testInfo.skip(
    !realStackEnabled,
    "Set E2E_REAL_STACK=1 to run against the real Web API and PostgreSQL stack.",
  );
}

export function requiredEnvironment(name: string): string {
  const value = process.env[name]?.trim();
  if (!value)
    throw new Error(
      `${name} is required when E2E_REAL_STACK=1. Use disposable test-stack data only.`,
    );
  return value;
}

export async function signIn(
  page: Page,
  emailVariable: string,
  passwordVariable: string,
): Promise<void> {
  await page.goto("/sign-in");
  await page
    .getByLabel("Email address")
    .fill(requiredEnvironment(emailVariable));
  await page.getByLabel("Password").fill(requiredEnvironment(passwordVariable));
  await page.getByRole("button", { name: /^sign in$/i }).click();
  await expect(page).not.toHaveURL(/\/sign-in(?:\?|$)/);
}
