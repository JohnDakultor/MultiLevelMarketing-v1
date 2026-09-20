import { expect, test } from "@playwright/test";
import { requireRealStack, requiredEnvironment, signIn } from "./realStack";

test.beforeEach(async ({}, testInfo) => {
  requireRealStack(testInfo, "agent-portal");
});

test("Agent dashboard, network, referral, wallet, and payout request", async ({
  page,
}) => {
  await signIn(page, "E2E_AGENT_EMAIL", "E2E_AGENT_PASSWORD");
  await expect(
    page.getByRole("heading", { name: "Agent dashboard" }),
  ).toBeVisible();

  await page.goto("/network");
  await expect(page.getByRole("heading", { name: "Network" })).toBeVisible();
  await page.goto("/referrals");
  await expect(
    page.getByRole("heading", { name: "Referral tools" }),
  ).toBeVisible();
  await expect(page.getByText(/referral/i).first()).toBeVisible();
  await page.goto("/wallet");
  await expect(page.getByRole("heading", { name: "Wallet" })).toBeVisible();

  await page.goto("/payouts");
  await page.getByLabel("Payout account").selectOption({ index: 1 });
  await page
    .getByLabel(/Amount \(/)
    .fill(requiredEnvironment("E2E_PAYOUT_AMOUNT"));
  await page.getByRole("button", { name: "Request payout" }).click();
  await expect(page.getByText("Payout requested.")).toBeVisible();
});
