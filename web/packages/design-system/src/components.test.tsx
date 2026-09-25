import "./testDom";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { cleanup, render, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { ApplicationShell } from "./ApplicationShell";
import { ConfirmationProvider, useConfirmation } from "./ConfirmationProvider";
import { DataTable } from "./DataTable";
import {
  EmptyState,
  ErrorState,
  FormErrorSummary,
  PermissionDeniedState,
} from "./Feedback";
import { InputField } from "./FormControls";
import { Pagination } from "./Navigation";

afterEach(cleanup);

test("forms expose backend validation errors to assistive technology", () => {
  const view = render(
    <>
      <FormErrorSummary
        errors={{ email: ["Enter a valid email address."] }}
        generalErrors={["The form could not be saved."]}
      />
      <InputField
        name="email"
        label="Email address"
        required
        error="Enter a valid email address."
      />
    </>,
  );

  const input = view.getByRole("textbox", { name: /email address/i });
  assert.equal(input.getAttribute("aria-invalid"), "true");
  assert.equal(input.getAttribute("aria-describedby"), "email-error");
  assert.ok(view.getAllByRole("alert").length >= 2);
  assert.equal(view.getByRole("link").getAttribute("href"), "#email");
});

test("data tables and pagination expose structure and bounded controls", async () => {
  const user = userEvent.setup();
  const visited: number[] = [];
  const view = render(
    <>
      <DataTable
        caption="Orders"
        rows={[{ id: "1", number: "ORD-1" }]}
        rowKey={(row) => row.id}
        columns={[
          { key: "number", header: "Order", cell: (row) => row.number },
        ]}
      />
      <Pagination
        page={2}
        totalPages={3}
        onPageChange={(page) => visited.push(page)}
      />
    </>,
  );

  assert.ok(view.getByRole("region", { name: "Orders" }));
  assert.equal(view.getByRole("columnheader").textContent, "Order");
  await user.click(view.getByRole("button", { name: "Previous" }));
  await user.click(view.getByRole("button", { name: "Next" }));
  assert.deepEqual(visited, [1, 3]);
});

test("confirmation dialogs resolve actions and move focus into the modal", async () => {
  const user = userEvent.setup();

  function Harness() {
    const confirmation = useConfirmation();
    const [answer, setAnswer] = useState("unanswered");
    return (
      <>
        <button
          type="button"
          onClick={async () => {
            const confirmed = await confirmation.confirm({
              title: "Reject application?",
              description: "This closes the pending application.",
              confirmLabel: "Reject application",
            });
            setAnswer(confirmed ? "confirmed" : "cancelled");
          }}
        >
          Review application
        </button>
        <output>{answer}</output>
      </>
    );
  }

  const view = render(
    <ConfirmationProvider>
      <Harness />
    </ConfirmationProvider>,
  );
  await user.click(view.getByRole("button", { name: "Review application" }));
  const dialog = view.getByRole("dialog", { name: "Reject application?" });
  assert.ok(dialog.hasAttribute("open"));
  await waitFor(() =>
    assert.equal(
      document.activeElement?.getAttribute("aria-label"),
      "Close dialog",
    ),
  );
  await user.click(view.getByRole("button", { name: "Reject application" }));
  assert.equal(view.getByText("confirmed").textContent, "confirmed");
});

test("loading-adjacent empty, forbidden, conflict, and failure states are explicit", () => {
  const view = render(
    <>
      <EmptyState
        title="No orders"
        description="Place an order to see it here."
      />
      <PermissionDeniedState />
      <ErrorState
        title="Inventory conflict"
        description="Refresh before trying the adjustment again."
      />
    </>,
  );

  assert.ok(view.getByRole("heading", { name: "No orders" }));
  assert.ok(view.getByRole("heading", { name: "Access denied" }));
  assert.ok(view.getByRole("heading", { name: "Inventory conflict" }));
  assert.equal(view.getAllByRole("alert").length, 2);
});

test("mobile navigation is labelled and restores focus when closed", async () => {
  const user = userEvent.setup();
  const view = render(
    <ApplicationShell
      variant="storefront"
      brand={{ name: "GreenZero", contextLabel: "Store" }}
      navigation={[{ href: "/products", label: "Products" }]}
      currentPath="/products"
    >
      <h1>Catalog</h1>
    </ApplicationShell>,
  );

  const openButton = view.getByRole("button", { name: "Open navigation" });
  await user.click(openButton);
  assert.ok(
    view.getByRole("dialog", { name: "Navigation" }).hasAttribute("open"),
  );
  await user.click(view.getByRole("button", { name: "Close" }));
  await waitFor(() => assert.equal(document.activeElement, openButton));
  assert.equal(
    view
      .getAllByRole("link", { name: "Products" })[0]
      ?.getAttribute("aria-current"),
    "page",
  );
});
