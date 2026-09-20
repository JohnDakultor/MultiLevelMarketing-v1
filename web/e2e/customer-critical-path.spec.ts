import { expect, test } from "@playwright/test";
import { requireRealStack, requiredEnvironment, signIn } from "./realStack";

test.beforeEach(async ({}, testInfo) => {
  requireRealStack(testInfo, "storefront");
});

test("customer referral, cart, authentication, checkout, return, cancellation, and refund", async ({
  page,
}) => {
  const referralCode = requiredEnvironment("E2E_REFERRAL_CODE");
  const productSlug = requiredEnvironment("E2E_PRODUCT_SLUG");

  await page.goto(`/r/${encodeURIComponent(referralCode)}`);
  await expect(page.getByText("Referral attribution applied.")).toBeVisible();
  await page.goto(`/products/${encodeURIComponent(productSlug)}`);
  await page.getByRole("button", { name: "Add to cart" }).click();
  await expect(page.getByText("Added to your cart.")).toBeVisible();
  await page.goto("/cart");
  await expect(page.getByRole("heading", { name: "Your cart" })).toBeVisible();

  await signIn(page, "E2E_CUSTOMER_EMAIL", "E2E_CUSTOMER_PASSWORD");
  await page.goto("/checkout");
  const checkout = page.getByRole("button", {
    name: "Continue to secure payment",
  });
  await expect(checkout).toBeEnabled();
  await checkout.click();
  await page.waitForURL(
    (url) => url.origin !== new URL(test.info().project.use.baseURL!).origin,
    { timeout: 30_000 },
  );

  const paidOrderId = requiredEnvironment("E2E_PAID_ORDER_ID");
  await page.goto(`/checkout/result?orderId=${paidOrderId}&return=succeeded`);
  await expect(page.getByRole("heading")).toContainText(/payment|order/i);

  const cancellableOrderId = requiredEnvironment("E2E_CANCELLABLE_ORDER_ID");
  await page.goto(`/account/orders/${cancellableOrderId}`);
  await page
    .getByLabel("Reason")
    .fill("Automated disposable-stack cancellation test");
  await page.getByRole("button", { name: "Request cancellation" }).click();
  await page
    .getByRole("dialog", { name: "Request order cancellation?" })
    .getByRole("button", { name: "Request cancellation" })
    .click();
  await expect(page.getByText("Cancellation requested.")).toBeVisible();

  const refundableOrderId = requiredEnvironment("E2E_REFUNDABLE_ORDER_ID");
  await page.goto(`/account/orders/${refundableOrderId}`);
  await page.getByText("Request item refund").first().click();
  await page
    .getByLabel("Reason")
    .first()
    .fill("Automated disposable-stack refund test");
  await page
    .getByRole("button", { name: "Submit refund request" })
    .first()
    .click();
  await expect(page.getByText("Refund requested.")).toBeVisible();
});
