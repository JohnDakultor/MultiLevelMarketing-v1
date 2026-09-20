import "@modular-mlm/design-system/test-dom";
import assert from "node:assert/strict";
import { afterEach, test } from "node:test";
import { ApiClient, ApiClientProvider } from "@modular-mlm/api-client";
import { ConfirmationProvider } from "@modular-mlm/design-system";
import { OrganizationProvider } from "@modular-mlm/organization-context";
import { cleanup, render, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { AgentOperationsPage } from "./agents/AgentOperationsPage";
import { CatalogInventoryPage } from "./catalog/CatalogInventoryPage";
import { AdminOrdersPage } from "./orders/AdminOrdersPage";
import { OrganizationSettingsPage } from "./organization/OrganizationSettingsPage";
import { PayoutAdministrationPage } from "./payouts/PayoutAdministrationPage";

afterEach(cleanup);

const organization = {
  id: "00000000-0000-0000-0000-000000000010",
  name: "Test Organization",
  slug: "test",
  currencyCode: "PHP",
  locale: "en-PH",
  storeTitle: "Test Store",
  primaryColor: "#173f35",
  secondaryColor: "#ffffff",
  accentColor: "#d79543",
  logoUrl: null,
  agentProgramEnabled: true,
  binaryNetworkEnabled: true,
  walletEnabled: true,
  payoutEnabled: true,
};

function responseFor(url: string): Response {
  if (url === "/api/organizations/public-config")
    return Response.json(organization);
  if (url.includes("/admin/products/categories"))
    return Response.json({ items: [], page: 1, pageSize: 100, totalCount: 0 });
  if (url.includes("/products/commission-profiles")) return Response.json([]);
  if (url.includes("/admin/products")) return Response.json([]);
  if (url.includes("/admin/inventory"))
    return Response.json({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasNextPage: false,
    });
  if (url.includes("/admin/agents/applications"))
    return Response.json({ items: [], page: 1, pageSize: 50, totalCount: 0 });
  if (url.includes("/admin/agents"))
    return Response.json({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      hasNextPage: false,
    });
  if (url.includes("/admin/payouts")) return Response.json([]);
  if (url.includes("/admin/orders"))
    return Response.json({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  if (url.endsWith("/wallet-settings"))
    return Response.json({
      commissionReleaseTrigger: 0,
      releaseDelayDays: 0,
      returnWindowDays: 7,
      minimumPayoutAmount: 500,
      allowNegativeRecoverableBalance: true,
      maximumNegativeBalance: 1000,
    });
  if (url.endsWith("/admin/settings"))
    return Response.json({
      profile: {
        id: organization.id,
        name: organization.name,
        slug: organization.slug,
        status: 0,
        currencyCode: "PHP",
        timeZone: "Asia/Manila",
        locale: "en-PH",
      },
      branding: {
        logoUrl: null,
        faviconUrl: null,
        primaryColor: "#173f35",
        secondaryColor: "#ffffff",
        accentColor: "#d79543",
        storeTitle: "Test Store",
        supportEmail: "support@example.test",
        supportPhone: null,
        footerText: null,
        revision: 1,
        publishedRevision: 1,
        publishedAt: null,
      },
      features: {
        commerceEnabled: true,
        agentProgramEnabled: true,
        binaryNetworkEnabled: true,
        binaryPairingEnabled: true,
        walletEnabled: true,
        payoutEnabled: true,
        reviewsEnabled: false,
        couponsEnabled: false,
      },
      commerce: {
        allowGuestCheckout: true,
        requireShippingAddress: true,
        requireBillingAddress: true,
        inventoryReservationMinutes: 15,
      },
      network: {
        defaultPlacementStrategy: 0,
        allowAgentPreferredLeg: true,
        maxQueryDepth: 10,
        autoPlacementEnabled: true,
        restrictPlacementChangesAfterActivation: true,
      },
      domains: [],
    });
  throw new Error(`Unexpected request: ${url}`);
}

function renderAdmin(
  component: ReactNode,
  override?: (url: string) => Response | undefined,
) {
  const api = new ApiClient({
    fetchImplementation: async (input) =>
      override?.(String(input)) ?? responseFor(String(input)),
  });
  return render(
    <ConfirmationProvider>
      <ApiClientProvider client={api}>
        <OrganizationProvider api={api} hostName="test.example">
          {component}
        </OrganizationProvider>
      </ApiClientProvider>
    </ConfirmationProvider>,
  );
}

test("Step 3 Admin screens render explicit empty states without raw identifier inputs", async () => {
  const cases: Array<[ReactNode, string]> = [
    [<AdminOrdersPage key="orders" />, "No matching orders"],
    [<CatalogInventoryPage key="catalog" />, "No products"],
    [<AgentOperationsPage key="agents" />, "No applications"],
    [<PayoutAdministrationPage key="payouts" />, "No payout requests"],
  ];
  for (const [component, emptyState] of cases) {
    const view = renderAdmin(component);
    assert.ok(await view.findByRole("heading", { name: emptyState }));
    assert.equal(
      view.queryByLabelText(/order id|payment id|refund id|agent id/i),
      null,
    );
    cleanup();
  }
});

test("Organization settings expose the network policy returned by the API", async () => {
  const view = renderAdmin(<OrganizationSettingsPage />);
  assert.ok(await view.findByRole("heading", { name: "Network" }));
});

test("Admin order permission failures render a forbidden state", async () => {
  const view = renderAdmin(<AdminOrdersPage />, (url) =>
    url.includes("/admin/orders")
      ? Response.json({ title: "Forbidden", status: 403 }, { status: 403 })
      : undefined,
  );
  await waitFor(() => assert.ok(view.getByText(/permission/i)));
});

test("Admin order search renders selected records returned by the tenant endpoint", async () => {
  const view = renderAdmin(<AdminOrdersPage />, (url) =>
    url.includes("/admin/orders")
      ? Response.json({
          items: [
            {
              id: "order-1",
              orderNumber: "ORD-1001",
              customerId: "customer-1",
              customerDisplayName: "Maria Santos",
              status: 1,
              paymentStatus: 2,
              currency: "PHP",
              grandTotal: 1250,
              itemCount: 2,
              createdAt: "2026-09-20T00:00:00Z",
              paidAt: null,
              deliveredAt: null,
            },
          ],
          page: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false,
        })
      : undefined,
  );
  assert.ok(await view.findByText("ORD-1001"));
  assert.ok(view.getByText("Maria Santos"));
  assert.ok(view.getByRole("button", { name: "View details" }));
});

test("Catalog, Agent, and payout screens render successful tenant responses", async () => {
  const catalog = renderAdmin(<CatalogInventoryPage />, (url) =>
    url.includes("/admin/products?page=")
      ? Response.json([
          {
            id: "product-1",
            categoryId: "category-1",
            categoryName: "Wellness",
            name: "Daily Greens",
            slug: "daily-greens",
            status: 1,
            price: 850,
            businessVolume: 75,
            stockQuantity: 12,
          },
        ])
      : undefined,
  );
  assert.ok(await catalog.findByText("Daily Greens"));
  cleanup();

  const agents = renderAdmin(<AgentOperationsPage />, (url) => {
    if (url.includes("/admin/agents/applications"))
      return Response.json({
        items: [
          {
            agentId: "agent-1",
            displayName: "Ana Reyes",
            email: "ana@example.test",
            agentCode: "AGT-100",
            status: 1,
            sponsorAgentId: null,
            sponsorAgentCode: null,
            joinedAt: "2026-09-20T00:00:00Z",
            isPlaced: false,
          },
        ],
        page: 1,
        pageSize: 50,
        totalCount: 1,
      });
    if (url.includes("/admin/agents?"))
      return Response.json({
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        hasNextPage: false,
      });
    return undefined;
  });
  assert.ok(await agents.findByText("AGT-100"));
  cleanup();

  const payouts = renderAdmin(<PayoutAdministrationPage />, (url) =>
    url.includes("/admin/payouts?page=")
      ? Response.json([
          {
            id: "payout-request-1",
            agentId: "agent-1",
            amount: 1200,
            currency: "PHP",
            status: 0,
            requestedAt: "2026-09-20T00:00:00Z",
            processedAt: null,
            providerReference: null,
          },
        ])
      : undefined,
  );
  assert.ok(await payouts.findByText("payout-r"));
});

test("Admin workflow screens distinguish forbidden responses from empty data", async () => {
  const cases: Array<[ReactNode, (url: string) => boolean]> = [
    [
      <CatalogInventoryPage key="catalog" />,
      (url) => url.includes("/admin/products?page="),
    ],
    [
      <AgentOperationsPage key="agents" />,
      (url) => url.includes("/admin/agents?"),
    ],
    [
      <PayoutAdministrationPage key="payouts" />,
      (url) => url.includes("/admin/payouts?page="),
    ],
  ];
  for (const [component, matches] of cases) {
    const view = renderAdmin(component, (url) =>
      matches(url)
        ? Response.json({ title: "Forbidden", status: 403 }, { status: 403 })
        : undefined,
    );
    await waitFor(() => assert.ok(view.getByText(/permission/i)));
    cleanup();
  }
});
