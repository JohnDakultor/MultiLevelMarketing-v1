"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import {
  Button,
  Card,
  DataTable,
  Dialog,
  EmptyState,
  PageHeader,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date, money } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";

export function SalesPage() {
  const scope = useAgentScope();
  const [view, setView] = useState<"orders" | "products">("orders");
  const [page, setPage] = useState(1);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const sales = useApiQuery(
    (api, signal) => agentApi.sales(api, scope.organizationId, page, signal),
    [scope.organizationId, page],
    scope.isReady && view === "orders",
  );
  const products = useApiQuery(
    (api, signal) =>
      agentApi.productSales(api, scope.organizationId, page, signal),
    [scope.organizationId, page],
    scope.isReady && view === "products",
  );
  const details = useApiQuery(
    (api, signal) =>
      agentApi.saleDetails(
        api,
        scope.organizationId,
        selectedOrderId ?? "",
        signal,
      ),
    [scope.organizationId, selectedOrderId],
    scope.isReady && selectedOrderId !== null,
  );

  const activeQuery = view === "orders" ? sales : products;
  if (activeQuery.isLoading) return <Loading />;
  if (activeQuery.error)
    return <Failure error={activeQuery.error} retry={activeQuery.reload} />;

  const selectView = (next: "orders" | "products") => {
    setView(next);
    setPage(1);
    setSelectedOrderId(null);
  };

  return (
    <div className="content-stack">
      <PageHeader
        title="Attributed sales"
        description="Customer names are privacy-masked by the backend. Order and product totals include only your attributed sales."
      />
      <div className="toolbar" aria-label="Sales views">
        <Button
          variant={view === "orders" ? "primary" : "secondary"}
          onClick={() => selectView("orders")}
        >
          Orders
        </Button>
        <Button
          variant={view === "products" ? "primary" : "secondary"}
          onClick={() => selectView("products")}
        >
          Products
        </Button>
      </div>

      {view === "orders" ? (
        !sales.data?.items.length ? (
          <EmptyState
            title="No attributed orders"
            description="Orders using your valid referral attribution will appear here."
          />
        ) : (
          <DataTable
            caption="Attributed orders"
            rows={sales.data.items}
            rowKey={(row) => row.orderId}
            columns={[
              { key: "order", header: "Order", cell: (row) => row.orderNumber },
              {
                key: "customer",
                header: "Customer",
                cell: (row) => row.maskedCustomerName,
              },
              {
                key: "created",
                header: "Created",
                cell: (row) => date(row.createdAt),
              },
              {
                key: "products",
                header: "Products",
                cell: (row) => row.productNames.join(", "),
              },
              { key: "bv", header: "BV", cell: (row) => row.businessVolume },
              {
                key: "commission",
                header: "Commission",
                align: "end",
                cell: (row) => money(row.commissionAmount, row.currency),
              },
              {
                key: "details",
                header: "",
                cell: (row) => (
                  <Button
                    variant="secondary"
                    onClick={() => setSelectedOrderId(row.orderId)}
                  >
                    View details
                  </Button>
                ),
              },
            ]}
          />
        )
      ) : !products.data?.items.length ? (
        <EmptyState
          title="No product sales"
          description="Product-level attributed sales will appear after paid referred orders are processed."
        />
      ) : (
        <DataTable
          caption="Attributed product sales"
          rows={products.data.items}
          rowKey={(row) => `${row.productId}-${row.productVariantId}`}
          columns={[
            {
              key: "product",
              header: "Product",
              cell: (row) => (
                <span>
                  <strong>{row.productName}</strong>
                  <br />
                  <small>{row.sku}</small>
                </span>
              ),
            },
            {
              key: "quantity",
              header: "Quantity",
              cell: (row) => row.quantitySold,
            },
            { key: "bv", header: "BV", cell: (row) => row.businessVolume },
            {
              key: "sales",
              header: "Attributed sales",
              cell: (row) => money(row.grossAttributedSales, row.currency),
            },
            {
              key: "commission",
              header: "Commission",
              align: "end",
              cell: (row) => money(row.commissionAmount, row.currency),
            },
          ]}
        />
      )}

      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!activeQuery.data?.hasPreviousPage}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {activeQuery.data?.page ?? page}</span>
        <Button
          variant="secondary"
          disabled={!activeQuery.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>

      <Dialog
        isOpen={selectedOrderId !== null}
        title={
          details.data ? `Order ${details.data.orderNumber}` : "Order details"
        }
        description="Privacy-safe details for an order attributed to your Agent account."
        onClose={() => setSelectedOrderId(null)}
      >
        {details.isLoading ? (
          <Loading />
        ) : details.error ? (
          <Failure error={details.error} retry={details.reload} />
        ) : details.data ? (
          <div className="content-stack">
            <div className="metric-grid">
              <Card>
                <span>Customer</span>
                <strong>{details.data.maskedCustomerName}</strong>
              </Card>
              <Card>
                <span>Total</span>
                <strong>
                  {money(details.data.grandTotal, details.data.currency)}
                </strong>
              </Card>
              <Card>
                <span>Created</span>
                <strong>{date(details.data.createdAt)}</strong>
              </Card>
            </div>
            <DataTable
              caption="Attributed order items"
              rows={details.data.items}
              rowKey={(row) => row.orderItemId}
              columns={[
                {
                  key: "product",
                  header: "Product",
                  cell: (row) => row.productName,
                },
                { key: "sku", header: "SKU", cell: (row) => row.sku },
                {
                  key: "quantity",
                  header: "Quantity",
                  cell: (row) => row.quantity,
                },
                { key: "bv", header: "BV", cell: (row) => row.businessVolume },
                {
                  key: "fulfillment",
                  header: "Fulfillment",
                  cell: (row) => row.fulfillmentStatus,
                },
              ]}
            />
            {!details.data.commissions.length ? (
              <EmptyState
                title="No commissions for this order"
                description="Commission records appear after compensation processing."
              />
            ) : (
              <DataTable
                caption="Order commissions"
                rows={details.data.commissions}
                rowKey={(row) => row.commissionId}
                columns={[
                  { key: "type", header: "Type", cell: (row) => row.type },
                  {
                    key: "status",
                    header: "Status",
                    cell: (row) => row.status,
                  },
                  {
                    key: "amount",
                    header: "Amount",
                    align: "end",
                    cell: (row) => money(row.amount, details.data!.currency),
                  },
                ]}
              />
            )}
          </div>
        ) : null}
      </Dialog>
    </div>
  );
}
