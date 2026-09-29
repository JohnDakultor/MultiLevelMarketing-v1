"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import { OrderStatus } from "@modular-mlm/contracts";
import {
  Button,
  DataTable,
  Dialog,
  EmptyState,
  PageHeader,
  InputField,
  SelectField,
  StatsCard,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date, money } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";
import {
  CommissionStatusBadge,
  OrderStatusBadge,
  commissionTypeLabel,
  fulfillmentStatusLabel,
  paymentStatusLabel,
} from "../shared/status";

export function SalesPage() {
  const scope = useAgentScope();
  const [view, setView] = useState<"orders" | "products">("orders");
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const sales = useApiQuery(
    (api, signal) =>
      agentApi.sales(
        api,
        scope.organizationId,
        page,
        {
          status: status === "" ? undefined : Number(status),
          from: from ? new Date(`${from}T00:00:00`).toISOString() : undefined,
          to: to ? new Date(`${to}T23:59:59.999`).toISOString() : undefined,
        },
        signal,
      ),
    [scope.organizationId, page, status, from, to],
    scope.isReady && view === "orders",
  );
  const products = useApiQuery(
    (api, signal) =>
      agentApi.productSales(
        api,
        scope.organizationId,
        page,
        {
          from: from ? new Date(`${from}T00:00:00`).toISOString() : undefined,
          to: to ? new Date(`${to}T23:59:59.999`).toISOString() : undefined,
        },
        signal,
      ),
    [scope.organizationId, page, from, to],
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
      <section className="filter-panel" aria-label="Sales filters">
        {view === "orders" && (
          <SelectField
            id="sales-status"
            label="Order status"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All statuses</option>
            {Object.values(OrderStatus).map((value) => (
              <option key={value} value={value}>
                {orderStatusLabel(value)}
              </option>
            ))}
          </SelectField>
        )}
        <InputField
          id="sales-from"
          label="From"
          type="date"
          value={from}
          onChange={(event) => {
            setFrom(event.target.value);
            setPage(1);
          }}
        />
        <InputField
          id="sales-to"
          label="To"
          type="date"
          min={from || undefined}
          value={to}
          onChange={(event) => {
            setTo(event.target.value);
            setPage(1);
          }}
        />
        {(status || from || to) && (
          <Button
            variant="ghost"
            onClick={() => {
              setStatus("");
              setFrom("");
              setTo("");
              setPage(1);
            }}
          >
            Clear filters
          </Button>
        )}
      </section>

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
                key: "status",
                header: "Status",
                cell: (row) => <OrderStatusBadge value={row.status} />,
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
              <StatsCard
                label="Customer"
                value={details.data.maskedCustomerName}
              />
              <StatsCard
                label="Total"
                value={money(details.data.grandTotal, details.data.currency)}
              />
              <StatsCard label="Created" value={date(details.data.createdAt)} />
            </div>
            <p className="status-line">
              <OrderStatusBadge value={details.data.status} />
              <span>
                Payment: {paymentStatusLabel(details.data.paymentStatus)}
              </span>
            </p>
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
                  cell: (row) => fulfillmentStatusLabel(row.fulfillmentStatus),
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
                  {
                    key: "type",
                    header: "Type",
                    cell: (row) => commissionTypeLabel(row.type),
                  },
                  {
                    key: "status",
                    header: "Status",
                    cell: (row) => <CommissionStatusBadge value={row.status} />,
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

function orderStatusLabel(value: number) {
  const labels = [
    "Pending payment",
    "Paid",
    "Processing",
    "Shipped",
    "Delivered",
    "Cancelled",
    "Partially refunded",
    "Refunded",
  ];
  return labels[value] ?? `Unknown (${value})`;
}
