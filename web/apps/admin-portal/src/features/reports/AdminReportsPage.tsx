"use client";
import { useApiQuery } from "@modular-mlm/api-client";
import {
  Card,
  DataTable,
  InputField,
  PageHeader,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, money, useAdminScope } from "../shared/AdminState";

const dateValue = (date: Date) => date.toISOString().slice(0, 10);
export function AdminReportsPage() {
  const scope = useAdminScope();
  const today = new Date();
  const prior = new Date();
  prior.setDate(today.getDate() - 30);
  const [from, setFrom] = useState(dateValue(prior));
  const [to, setTo] = useState(dateValue(today));
  const report = useApiQuery(
    (api, signal) =>
      adminApi.report(
        api,
        scope.organizationId,
        new Date(`${from}T00:00:00Z`).toISOString(),
        new Date(`${to}T23:59:59Z`).toISOString(),
        signal,
      ),
    [scope.organizationId, from, to],
    scope.isReady,
  );
  if (report.isLoading) return <Loading />;
  if (report.error)
    return <Failure error={report.error} retry={report.reload} />;
  const data = report.data;
  return (
    <div className="content-stack">
      <PageHeader
        title="Reports"
        description="Date-ranged totals and breakdowns are returned by the reporting API. No synthetic trend line is shown."
      />
      <div className="toolbar">
        <InputField
          id="report-from"
          label="From"
          type="date"
          value={from}
          max={to}
          onChange={(event) => setFrom(event.target.value)}
        />
        <InputField
          id="report-to"
          label="To"
          type="date"
          value={to}
          min={from}
          onChange={(event) => setTo(event.target.value)}
        />
      </div>
      <div className="metric-grid">
        <Metric
          label="Gross sales"
          value={money(data?.grossSales ?? 0, data?.currency ?? scope.currency)}
        />
        <Metric
          label="Net sales"
          value={money(data?.netSales ?? 0, data?.currency ?? scope.currency)}
        />
        <Metric
          label="Refunded"
          value={money(
            data?.refundedAmount ?? 0,
            data?.currency ?? scope.currency,
          )}
        />
        <Metric label="Orders" value={String(data?.orderCount ?? 0)} />
        <Metric
          label="Business volume"
          value={String(data?.businessVolume ?? 0)}
        />
        <Metric
          label="Commission expense"
          value={money(
            data?.commissionExpense ?? 0,
            data?.currency ?? scope.currency,
          )}
        />
      </div>
      <div className="detail-grid">
        <Card>
          <h2>Sales by product</h2>
          <DataTable
            caption="Sales by product"
            rows={data?.salesByProduct ?? []}
            rowKey={(row) => row.id}
            columns={[
              { key: "name", header: "Product", cell: (row) => row.label },
              { key: "orders", header: "Orders", cell: (row) => row.orders },
              {
                key: "sales",
                header: "Sales",
                align: "end",
                cell: (row) =>
                  money(row.grossSales, data?.currency ?? scope.currency),
              },
            ]}
          />
        </Card>
        <Card>
          <h2>Sales by Agent</h2>
          <DataTable
            caption="Sales by Agent"
            rows={data?.salesByAgent ?? []}
            rowKey={(row) => row.id}
            columns={[
              { key: "name", header: "Agent", cell: (row) => row.label },
              { key: "orders", header: "Orders", cell: (row) => row.orders },
              {
                key: "sales",
                header: "Sales",
                align: "end",
                cell: (row) =>
                  money(row.grossSales, data?.currency ?? scope.currency),
              },
            ]}
          />
        </Card>
      </div>
    </div>
  );
}
function Metric({ label, value }: { label: string; value: string }) {
  return (
    <Card>
      <span>{label}</span>
      <strong className="metric-value">{value}</strong>
    </Card>
  );
}
