"use client";

import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import type { AdminOrderDetailsDto } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  SelectField,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import { adminApi } from "../api/adminApi";
import {
  Failure,
  Loading,
  date,
  money,
  useAdminScope,
} from "../shared/AdminState";

export function AdminOrdersPage() {
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [paymentStatus, setPaymentStatus] = useState("");
  const [createdFrom, setCreatedFrom] = useState("");
  const [createdTo, setCreatedTo] = useState("");
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [message, setMessage] = useState("");
  const parameters = new URLSearchParams({
    page: String(page),
    pageSize: "20",
  });
  if (search.trim()) parameters.set("search", search.trim());
  if (status) parameters.set("status", status);
  if (paymentStatus) parameters.set("paymentStatus", paymentStatus);
  if (createdFrom)
    parameters.set("createdFrom", new Date(createdFrom).toISOString());
  if (createdTo)
    parameters.set(
      "createdTo",
      new Date(`${createdTo}T23:59:59`).toISOString(),
    );
  const orders = useApiQuery(
    (client, signal) =>
      adminApi.orders(client, scope.organizationId, parameters, signal),
    [
      scope.organizationId,
      page,
      search,
      status,
      paymentStatus,
      createdFrom,
      createdTo,
    ],
    scope.isReady,
  );
  if (orders.isLoading) return <Loading />;
  if (orders.error)
    return <Failure error={orders.error} retry={orders.reload} />;
  const refresh = (text: string) => {
    setMessage(text);
    orders.reload();
  };
  return (
    <div className="content-stack">
      <PageHeader
        title="Orders and refunds"
        description="Find organization orders, inspect payment and item-refund history, and perform audited financial operations."
      />
      {message && <Alert title={message} tone="success" />}
      <div className="toolbar">
        <InputField
          id="order-search"
          label="Search order or customer"
          value={search}
          onChange={(event) => {
            setPage(1);
            setSearch(event.target.value);
          }}
        />
        <SelectField
          id="order-status"
          label="Order status"
          value={status}
          onChange={(event) => {
            setPage(1);
            setStatus(event.target.value);
          }}
        >
          <option value="">All statuses</option>
          {[0, 1, 2, 3, 4, 5, 6, 7].map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </SelectField>
        <SelectField
          id="payment-status"
          label="Payment status"
          value={paymentStatus}
          onChange={(event) => {
            setPage(1);
            setPaymentStatus(event.target.value);
          }}
        >
          <option value="">All payment statuses</option>
          {[0, 1, 2, 3, 4, 5].map((value) => (
            <option key={value} value={value}>
              {value}
            </option>
          ))}
        </SelectField>
        <InputField
          id="created-from"
          label="Created from"
          type="date"
          value={createdFrom}
          onChange={(event) => {
            setPage(1);
            setCreatedFrom(event.target.value);
          }}
        />
        <InputField
          id="created-to"
          label="Created to"
          type="date"
          value={createdTo}
          onChange={(event) => {
            setPage(1);
            setCreatedTo(event.target.value);
          }}
        />
      </div>
      {!orders.data?.items.length ? (
        <EmptyState
          title="No matching orders"
          description="Change the filters or wait for the first customer order."
        />
      ) : (
        <DataTable
          caption="Organization orders"
          rows={orders.data.items}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "order",
              header: "Order",
              cell: (row) => (
                <>
                  <strong>{row.orderNumber}</strong>
                  <br />
                  <small>{row.customerDisplayName}</small>
                </>
              ),
            },
            {
              key: "created",
              header: "Created",
              cell: (row) => date(row.createdAt),
            },
            { key: "items", header: "Items", cell: (row) => row.itemCount },
            {
              key: "total",
              header: "Total",
              cell: (row) => money(row.grandTotal, row.currency),
            },
            {
              key: "status",
              header: "Order / payment",
              cell: (row) => `${row.status} / ${row.paymentStatus}`,
            },
            {
              key: "actions",
              header: "",
              cell: (row) => (
                <Button
                  variant="secondary"
                  onClick={() => setSelectedOrderId(row.id)}
                >
                  View details
                </Button>
              ),
            },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!orders.data?.hasPreviousPage}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>
          Page {orders.data?.page ?? page} of {orders.data?.totalPages ?? 1}
        </span>
        <Button
          variant="secondary"
          disabled={!orders.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
      {selectedOrderId && (
        <OrderDetails
          orderId={selectedOrderId}
          close={() => setSelectedOrderId(null)}
          saved={refresh}
        />
      )}
    </div>
  );
}

function OrderDetails({
  orderId,
  close,
  saved,
}: {
  orderId: string;
  close(): void;
  saved(message: string): void;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  const { confirm, prompt } = useConfirmation();
  const feedback = useFormSubmission();
  const details = useApiQuery(
    (client, signal) =>
      adminApi.orderDetails(client, scope.organizationId, orderId, signal),
    [scope.organizationId, orderId],
    scope.isReady,
  );
  if (details.isLoading) return <Loading />;
  if (details.error)
    return <Failure error={details.error} retry={details.reload} />;
  if (!details.data)
    return (
      <EmptyState
        title="Order unavailable"
        description="The order no longer exists in this organization."
      />
    );
  const data = details.data;
  async function run(
    operation: () => Promise<unknown>,
    confirmation: Confirmation,
    success: string,
  ) {
    if (
      !(await confirm({
        title: confirmation.title,
        description: confirmation.description,
        confirmLabel: confirmation.label,
      }))
    )
      return;
    const completed = await feedback.submit(operation, success);
    if (completed) {
      details.reload();
      saved(success);
    }
  }
  return (
    <Card>
      <div className="action-row">
        <div>
          <h2>Order {data.orderNumber}</h2>
          <p>
            {data.customerDisplayName} · {date(data.createdAt)} ·{" "}
            {money(data.grandTotal, data.currency)}
          </p>
        </div>
        <Button variant="ghost" onClick={close}>
          Close
        </Button>
      </div>
      <FormErrorSummary
        errors={feedback.fieldErrors}
        generalErrors={feedback.formErrors}
        id={feedback.errorSummaryId}
      />
      <div className="button-cluster" aria-label="Order fulfillment actions">
        {data.status === 1 && (
          <Button onClick={() => void run(
            () => adminApi.startOrderProcessing(api, scope.organizationId, data.id),
            { title: "Start processing?", description: `Start fulfillment for ${data.orderNumber}?`, label: "Start processing" },
            "Order processing started.",
          )}>Start processing</Button>
        )}
        {data.status === 2 && (
          <Button onClick={async () => {
            const carrier = await prompt({ title: "Ship order", description: "Enter the shipping carrier.", label: "Carrier", submitLabel: "Continue", maxLength: 100 });
            if (!carrier) return;
            const tracking = await prompt({ title: "Tracking number", description: "Enter the carrier tracking number.", label: "Tracking number", submitLabel: "Mark shipped", maxLength: 200 });
            if (!tracking) return;
            await run(
              () => adminApi.shipOrder(api, scope.organizationId, data.id, carrier, tracking),
              { title: "Mark shipped?", description: `Ship ${data.orderNumber} with ${carrier}?`, label: "Mark shipped" },
              "Order marked shipped.",
            );
          }}>Mark shipped</Button>
        )}
        {data.status === 3 && (
          <Button onClick={() => void run(
            () => adminApi.deliverOrder(api, scope.organizationId, data.id),
            { title: "Mark delivered?", description: `Confirm delivery of ${data.orderNumber}?`, label: "Mark delivered" },
            "Order marked delivered.",
          )}>Mark delivered</Button>
        )}
        {data.status === 0 && (
          <Button variant="danger" onClick={async () => {
            const reason = await prompt({ title: "Cancel order", description: `Cancel unpaid order ${data.orderNumber}?`, label: "Reason", submitLabel: "Cancel order", maxLength: 500 });
            if (!reason) return;
            await run(
              () => adminApi.cancelOrder(api, scope.organizationId, data.id, reason),
              { title: "Cancel order?", description: "Inventory reservations will be released.", label: "Cancel order" },
              "Order cancelled.",
            );
          }}>Cancel order</Button>
        )}
      </div>
      <h3>Items</h3>
      <DataTable
        caption="Order items"
        rows={data.items}
        rowKey={(row) => row.id}
        columns={[
          {
            key: "product",
            header: "Product",
            cell: (row) => (
              <>
                {row.productName}
                <br />
                <small>{row.sku}</small>
              </>
            ),
          },
          { key: "quantity", header: "Quantity", cell: (row) => row.quantity },
          {
            key: "total",
            header: "Line total",
            cell: (row) => money(row.lineTotal, data.currency),
          },
          {
            key: "fulfillment",
            header: "Fulfillment",
            cell: (row) => row.fulfillmentStatus,
          },
          {
            key: "refund",
            header: "",
            cell: (row) => (
              <ItemRefundForm
                order={data}
                itemId={row.id}
                maximum={row.quantity}
                run={run}
              />
            ),
          },
        ]}
      />
      <h3>Payments</h3>
      {!data.payments.length ? (
        <p>No payment attempts recorded.</p>
      ) : (
        data.payments.map((payment) => (
          <div className="action-row" key={payment.id}>
            <span>
              <strong>{payment.provider}</strong> ·{" "}
              {money(payment.amount, payment.currency)} · status{" "}
              {payment.status}
              <br />
              <small>
                {payment.providerPaymentId ?? "Provider payment pending"}
              </small>
            </span>
            <div className="button-cluster">
              <PaymentRefundForm
                order={data}
                paymentAmount={payment.amount - payment.refundedAmount}
                run={run}
              />
              <Button
                variant="secondary"
                disabled={feedback.isSubmitting}
                onClick={() =>
                  void run(
                    () =>
                      adminApi.reconcilePayment(
                        api,
                        scope.organizationId,
                        payment.id,
                      ),
                    {
                      title: "Reconcile payment?",
                      description: `Request current provider state for payment on order ${data.orderNumber}?`,
                      label: "Reconcile payment",
                    },
                    "Payment reconciled.",
                  )
                }
              >
                Reconcile payment
              </Button>
            </div>
          </div>
        ))
      )}
      <h3>Item-refund history</h3>
      {!data.itemRefunds.length ? (
        <p>No item refunds recorded.</p>
      ) : (
        <DataTable
          caption="Item-refund history"
          rows={data.itemRefunds}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "date",
              header: "Requested",
              cell: (row) => date(row.requestedAt),
            },
            {
              key: "quantity",
              header: "Quantity",
              cell: (row) => row.quantity,
            },
            {
              key: "amount",
              header: "Amount",
              cell: (row) => money(row.amount, row.currency),
            },
            {
              key: "status",
              header: "Reversal / provider",
              cell: (row) => `${row.reversalStatus} / ${row.providerStatus}`,
            },
            {
              key: "reason",
              header: "Reason",
              cell: (row) => row.failureSummary ?? row.reason,
            },
            {
              key: "action",
              header: "",
              cell: (row) => (
                <Button
                  variant="secondary"
                  disabled={feedback.isSubmitting}
                  onClick={() =>
                    void run(
                      () =>
                        adminApi.reconcileItemRefund(
                          api,
                          scope.organizationId,
                          data.id,
                          row.id,
                        ),
                      {
                        title: "Reconcile item refund?",
                        description: `Reconcile the provider refund and inventory reversal for ${data.orderNumber}?`,
                        label: "Reconcile refund",
                      },
                      "Item refund reconciled.",
                    )
                  }
                >
                  Reconcile
                </Button>
              ),
            },
          ]}
        />
      )}
    </Card>
  );
}

interface Confirmation {
  title: string;
  description: string;
  label: string;
}
type FinancialRunner = (
  operation: () => Promise<unknown>,
  confirmation: Confirmation,
  success: string,
) => Promise<void>;

function PaymentRefundForm({
  order,
  paymentAmount,
  run,
}: {
  order: AdminOrderDetailsDto;
  paymentAmount: number;
  run: FinancialRunner;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  return (
    <details>
      <summary>Refund payment</summary>
      <form
        className="form-grid compact-form"
        onSubmit={(event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          const input = new FormData(event.currentTarget);
          const amount = Number(input.get("amount"));
          const reason = String(input.get("reason"));
          void run(
            () =>
              adminApi.requestPaymentRefund(
                api,
                scope.organizationId,
                order.id,
                amount,
                reason,
              ),
            {
              title: "Refund payment?",
              description: `Refund ${money(amount, order.currency)} from order ${order.orderNumber}?`,
              label: "Request refund",
            },
            "Payment refund requested.",
          );
        }}
      >
        <InputField
          name="amount"
          label="Refund amount"
          type="number"
          min="0.01"
          max={paymentAmount}
          step="0.01"
          required
        />
        <InputField name="reason" label="Reason" required />
        <Button type="submit" variant="danger">
          Request refund
        </Button>
      </form>
    </details>
  );
}

function ItemRefundForm({
  order,
  itemId,
  maximum,
  run,
}: {
  order: AdminOrderDetailsDto;
  itemId: string;
  maximum: number;
  run: FinancialRunner;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  return (
    <details>
      <summary>Refund item</summary>
      <form
        className="form-grid compact-form"
        onSubmit={(event: FormEvent<HTMLFormElement>) => {
          event.preventDefault();
          const input = new FormData(event.currentTarget);
          const quantity = Number(input.get("quantity"));
          const reason = String(input.get("reason"));
          void run(
            () =>
              adminApi.requestItemRefund(
                api,
                scope.organizationId,
                order.id,
                itemId,
                quantity,
                reason,
              ),
            {
              title: "Refund order item?",
              description: `Refund ${quantity} item(s) from order ${order.orderNumber}?`,
              label: "Request item refund",
            },
            "Item refund requested.",
          );
        }}
      >
        <InputField
          name="quantity"
          label="Quantity"
          type="number"
          min={1}
          max={maximum}
          required
        />
        <InputField name="reason" label="Reason" required />
        <Button type="submit" variant="danger">
          Request refund
        </Button>
      </form>
    </details>
  );
}
