import { expect, test } from "@playwright/test";
import { requireRealStack, requiredEnvironment, signIn } from "./realStack";

test.beforeEach(async ({}, testInfo) => {
  requireRealStack(testInfo, "admin-portal");
});

test("Administrator catalog, inventory, Agent review, payouts, reports, and audit", async ({
  page,
}) => {
  await signIn(page, "E2E_ADMIN_EMAIL", "E2E_ADMIN_PASSWORD");
  await expect(
    page.getByRole("heading", { name: "Operations dashboard" }),
  ).toBeVisible();

  await page.goto("/catalog");
  await expect(
    page.getByRole("heading", { name: "Catalog and inventory" }),
  ).toBeVisible();
  const createProduct = page.getByText("Create product");
  await createProduct.click();
  const unique = Date.now().toString(36);
  await page.getByLabel("Category").selectOption({ index: 1 });
  await page.getByLabel("Name").fill(`E2E Product ${unique}`);
  await page.getByLabel("Slug").fill(`e2e-product-${unique}`);
  await page
    .getByLabel("Description")
    .fill("Disposable-stack browser test product");
  await page.getByLabel("Initial SKU").fill(`E2E-${unique}`.toUpperCase());
  await page.getByLabel("Price").fill("100");
  await page.getByLabel("Business volume").fill("10");
  await page.getByLabel("Initial stock").fill("2");
  await page.getByRole("button", { name: "Create draft product" }).click();
  await expect(page.getByText("Product created as a draft.")).toBeVisible();

  const productRow = page.getByRole("row", {
    name: new RegExp(`E2E Product ${unique}`),
  });
  await productRow.getByRole("button", { name: "Publish" }).click();
  await expect(page.getByText(/published/i)).toBeVisible();

  await page.goto("/agents");
  const pendingCode = requiredEnvironment("E2E_PENDING_AGENT_CODE");
  const applicationRow = page.getByRole("row", {
    name: new RegExp(pendingCode, "i"),
  });
  await applicationRow.getByRole("button", { name: "Approve" }).click();
  await page
    .getByRole("dialog", { name: "approve Agent?" })
    .getByRole("button", { name: "approve Agent" })
    .click();
  await expect(
    page.getByText(new RegExp(`${pendingCode} approve request completed`, "i")),
  ).toBeVisible();

  await page.goto("/payouts");
  await expect(
    page.getByRole("heading", { name: "Payout administration" }),
  ).toBeVisible();
  await page.goto("/reports");
  await expect(page.getByRole("heading", { name: "Reports" })).toBeVisible();
  await page.goto("/audit");
  await expect(
    page.getByRole("heading", { name: "Audit trail" }),
  ).toBeVisible();
});

test("Administrator finds an order, requests a payment refund, and reconciles provider state", async ({
  page,
}) => {
  await signIn(page, "E2E_ADMIN_EMAIL", "E2E_ADMIN_PASSWORD");
  const orderNumber = requiredEnvironment("E2E_ADMIN_REFUNDABLE_ORDER_NUMBER");
  await page.goto("/orders");
  await page.getByLabel("Search order or customer").fill(orderNumber);
  const orderRow = page.getByRole("row", {
    name: new RegExp(orderNumber, "i"),
  });
  await orderRow.getByRole("button", { name: "View details" }).click();
  await expect(
    page.getByRole("heading", { name: `Order ${orderNumber}` }),
  ).toBeVisible();

  const paymentRefund = page
    .locator("details")
    .filter({ hasText: "Refund payment" })
    .first();
  await paymentRefund.getByText("Refund payment", { exact: true }).click();
  await paymentRefund
    .getByLabel("Refund amount")
    .fill(requiredEnvironment("E2E_ADMIN_PAYMENT_REFUND_AMOUNT"));
  await paymentRefund
    .getByLabel("Reason")
    .fill("Disposable-stack payment refund test");
  await paymentRefund.getByRole("button", { name: "Request refund" }).click();
  await page
    .getByRole("dialog", { name: "Refund payment?" })
    .getByRole("button", { name: "Request refund" })
    .click();
  await expect(page.getByText("Payment refund requested.")).toBeVisible();

  await page.getByRole("button", { name: "Reconcile payment" }).click();
  await page
    .getByRole("dialog", { name: "Reconcile payment?" })
    .getByRole("button", { name: "Reconcile payment" })
    .click();
  await expect(page.getByText("Payment reconciled.")).toBeVisible();
});

test("Administrator manually places an Agent using a discovered parent", async ({
  page,
}) => {
  await signIn(page, "E2E_ADMIN_EMAIL", "E2E_ADMIN_PASSWORD");
  const agentCode = requiredEnvironment("E2E_UNPLACED_AGENT_CODE");
  const parentCode = requiredEnvironment("E2E_PLACEMENT_PARENT_AGENT_CODE");
  await page.goto("/agents");
  await page.getByLabel("Search Agents").fill(agentCode);
  const agentRow = page.getByRole("row", { name: new RegExp(agentCode, "i") });
  await agentRow.getByRole("button", { name: "Details" }).click();
  const parentOption = page.getByLabel("Parent Agent").locator("option", {
    hasText: parentCode,
  });
  const parentId = await parentOption.getAttribute("value");
  if (!parentId)
    throw new Error(`Placement parent ${parentCode} was not discoverable.`);
  await page.getByLabel("Parent Agent").selectOption(parentId);
  await page.getByLabel("Placement side").selectOption("0");
  await page.getByRole("button", { name: "Place Agent", exact: true }).click();
  await page
    .getByRole("dialog", { name: "Place Agent" })
    .getByRole("button", { name: "Place Agent" })
    .click();
  await expect(page.getByText("Agent placement updated.")).toBeVisible();
});

for (const operation of ["verify", "reject"] as const) {
  test(`Administrator can ${operation} a payout account from a selected payout`, async ({
    page,
  }) => {
    await signIn(page, "E2E_ADMIN_EMAIL", "E2E_ADMIN_PASSWORD");
    const prefix = requiredEnvironment(
      operation === "verify"
        ? "E2E_VERIFY_PAYOUT_REQUEST_PREFIX"
        : "E2E_REJECT_PAYOUT_REQUEST_PREFIX",
    );
    await page.goto("/payouts");
    const payoutRow = page.getByRole("row", { name: new RegExp(prefix, "i") });
    await payoutRow.getByRole("button", { name: "Details" }).click();
    await page
      .getByRole("button", {
        name:
          operation === "verify"
            ? "Verify payout account"
            : "Reject payout account",
      })
      .click();
    await page
      .getByRole("dialog", { name: `${operation} payout account?` })
      .getByRole("button", { name: `${operation} account` })
      .click();
    await expect(
      page.getByText(`Payout account ${operation} completed.`),
    ).toBeVisible();
  });
}
