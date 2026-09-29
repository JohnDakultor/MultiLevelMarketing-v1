"use client";

import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import type { AdminCustomerSummaryDto } from "@modular-mlm/contracts";
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
  StatusBadge,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useDeferredValue, useState } from "react";
import { adminApi } from "../api/adminApi";
import {
  Failure,
  Loading,
  date,
  money,
  useAdminScope,
} from "../shared/AdminState";

const customerStatuses = {
  0: { label: "Active", tone: "success" as const },
  1: { label: "Suspended", tone: "warning" as const },
  2: { label: "Disabled", tone: "danger" as const },
};

function CustomerStatusBadge({ value }: { value: number }) {
  const status = customerStatuses[value as keyof typeof customerStatuses] ?? {
    label: "Unknown",
    tone: "neutral" as const,
  };
  return <StatusBadge label={status.label} tone={status.tone} />;
}

export function CustomerAdministrationPage() {
  const api = useApiClient();
  const scope = useAdminScope();
  const { prompt, confirm } = useConfirmation();
  const feedback = useFormSubmission();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [actingId, setActingId] = useState<string | null>(null);
  const [message, setMessage] = useState("");

  const customers = useApiQuery(
    (client, signal) =>
      adminApi.customers(
        client,
        scope.organizationId,
        page,
        deferredSearch,
        status,
        signal,
      ),
    [scope.organizationId, page, deferredSearch, status],
    scope.isReady,
  );
  const details = useApiQuery(
    (client, signal) =>
      adminApi.customer(client, scope.organizationId, selected ?? "", signal),
    [scope.organizationId, selected],
    scope.isReady && selected !== null,
  );

  async function changeStatus(customer: AdminCustomerSummaryDto, next: number) {
    const label =
      customerStatuses[next as keyof typeof customerStatuses]?.label;
    const reason = await prompt({
      title: `${label} customer`,
      description:
        "The reason is required and will be written to the audit trail.",
      label: "Reason",
      submitLabel: "Continue",
    });
    if (!reason) return;
    if (
      !(await confirm({
        title: `${label} ${customer.displayName}?`,
        description:
          "This changes which customer operations the account can perform.",
        confirmLabel: label,
      }))
    )
      return;

    setActingId(customer.id);
    const changed = await feedback.submit(
      () =>
        adminApi.changeCustomerStatus(
          api,
          scope.organizationId,
          customer.id,
          next,
          reason,
        ),
      `Customer status changed to ${label}.`,
    );
    setActingId(null);
    if (changed) {
      setMessage(`Customer status changed to ${label}.`);
      customers.reload();
      if (selected === customer.id) details.reload();
    }
  }

  if (customers.isLoading) return <Loading />;
  if (customers.error)
    return <Failure error={customers.error} retry={customers.reload} />;

  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Accounts"
        title="Customers"
        description="Search customer accounts, review safe account history, and control tenant access."
      />
      {message ? <Alert title={message} tone="success" /> : null}
      <FormErrorSummary
        errors={feedback.fieldErrors}
        generalErrors={feedback.formErrors}
        id={feedback.errorSummaryId}
      />
      <div className="toolbar ds-filter-bar">
        <InputField
          id="customer-search"
          label="Search customers"
          placeholder="Name or email"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setPage(1);
          }}
        />
        <SelectField
          id="customer-status"
          label="Status"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
            setPage(1);
          }}
        >
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Suspended">Suspended</option>
          <option value="Disabled">Disabled</option>
        </SelectField>
      </div>
      {!customers.data?.items.length ? (
        <EmptyState
          title="No customers found"
          description="Try changing the search or status filter. Customer profiles appear after a shopper signs in."
        />
      ) : (
        <DataTable
          caption="Tenant customers"
          rows={customers.data.items}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "customer",
              header: "Customer",
              cell: (row) => (
                <>
                  <strong>{row.displayName}</strong>
                  <br />
                  <small>{row.email || "Email unavailable"}</small>
                </>
              ),
            },
            {
              key: "status",
              header: "Status",
              cell: (row) => <CustomerStatusBadge value={row.status} />,
            },
            { key: "orders", header: "Orders", cell: (row) => row.orderCount },
            {
              key: "value",
              header: "Order value",
              cell: (row) =>
                row.currency
                  ? money(row.grossOrderValue, row.currency)
                  : String(row.grossOrderValue),
            },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => (
                <div className="button-row">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setSelected(row.id)}
                  >
                    Details
                  </Button>
                  {row.status === 0 ? (
                    <Button
                      variant="secondary"
                      size="sm"
                      isLoading={actingId === row.id}
                      disabled={feedback.isSubmitting}
                      onClick={() => void changeStatus(row, 1)}
                    >
                      Suspend
                    </Button>
                  ) : null}
                  {row.status === 1 ? (
                    <Button
                      variant="secondary"
                      size="sm"
                      isLoading={actingId === row.id}
                      disabled={feedback.isSubmitting}
                      onClick={() => void changeStatus(row, 0)}
                    >
                      Reactivate
                    </Button>
                  ) : null}
                  {row.status !== 2 ? (
                    <Button
                      variant="danger"
                      size="sm"
                      isLoading={actingId === row.id}
                      disabled={feedback.isSubmitting}
                      onClick={() => void changeStatus(row, 2)}
                    >
                      Disable
                    </Button>
                  ) : null}
                </div>
              ),
            },
          ]}
        />
      )}
      <div className="button-row">
        <Button
          variant="secondary"
          disabled={!customers.data?.hasPreviousPage}
          onClick={() => setPage((value) => Math.max(1, value - 1))}
        >
          Previous
        </Button>
        <span>
          Page {customers.data?.page ?? page} of{" "}
          {Math.max(customers.data?.totalPages ?? 0, 1)}
        </span>
        <Button
          variant="secondary"
          disabled={!customers.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
      {selected ? (
        <Card>
          <div className="button-row">
            <h2>Customer details</h2>
            <Button variant="ghost" onClick={() => setSelected(null)}>
              Close
            </Button>
          </div>
          {details.isLoading ? <Loading /> : null}
          {details.error ? (
            <Failure error={details.error} retry={details.reload} />
          ) : null}
          {details.data ? (
            <div className="content-stack">
              <div>
                <strong>{details.data.displayName}</strong> ·{" "}
                {details.data.email || "Email unavailable"}
              </div>
              <div className="button-row">
                <CustomerStatusBadge value={details.data.status} />
                <span>Registered {date(details.data.registeredAt)}</span>
                <span>{details.data.addressCount} addresses</span>
                <span>{details.data.orderCount} orders</span>
              </div>
              {!details.data.recentOrders.length ? (
                <EmptyState
                  title="No orders"
                  description="This customer has no order history."
                />
              ) : (
                <DataTable
                  caption="Recent customer orders"
                  rows={details.data.recentOrders}
                  rowKey={(row) => row.id}
                  columns={[
                    {
                      key: "number",
                      header: "Order",
                      cell: (row) => row.orderNumber,
                    },
                    {
                      key: "date",
                      header: "Created",
                      cell: (row) => date(row.created),
                    },
                    {
                      key: "total",
                      header: "Total",
                      cell: (row) => money(row.grandTotal, row.currency),
                    },
                  ]}
                />
              )}
            </div>
          ) : null}
        </Card>
      ) : null}
    </div>
  );
}
