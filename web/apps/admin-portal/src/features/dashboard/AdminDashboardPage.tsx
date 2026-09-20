"use client";
import { useApiQuery } from "@modular-mlm/api-client";
import { Alert, Card, PageHeader } from "@modular-mlm/design-system";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, money, useAdminScope } from "../shared/AdminState";

export function AdminDashboardPage() {
  const scope = useAdminScope();
  const dashboard = useApiQuery(
    (api, signal) => adminApi.dashboard(api, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  if (dashboard.isLoading) return <Loading />;
  if (dashboard.error)
    return <Failure error={dashboard.error} retry={dashboard.reload} />;
  const data = dashboard.data;
  return (
    <div className="content-stack">
      <PageHeader
        title="Operations dashboard"
        description="Actionable totals are backend aggregates observed at the displayed time."
      />
      <div className="metric-grid">
        <Metric
          label="Sales today"
          value={money(data?.salesToday ?? 0, data?.currency ?? scope.currency)}
        />
        <Metric
          label="Sales · 30 days"
          value={money(
            data?.salesLast30Days ?? 0,
            data?.currency ?? scope.currency,
          )}
        />
        <Metric label="Orders today" value={String(data?.ordersToday ?? 0)} />
        <Metric label="Active Agents" value={String(data?.activeAgents ?? 0)} />
        <Metric
          label="Commission liability"
          value={money(
            data?.commissionLiability ?? 0,
            data?.currency ?? scope.currency,
          )}
        />
        <Metric
          label="Wallet liability"
          value={money(
            data?.walletLiability ?? 0,
            data?.currency ?? scope.currency,
          )}
        />
      </div>
      {data?.alerts.map((alert) => (
        <Alert
          key={alert.code}
          title={alert.code}
          tone={
            alert.severity === "danger"
              ? "danger"
              : alert.severity === "warning"
                ? "warning"
                : "info"
          }
        >
          {alert.message}
        </Alert>
      ))}
      <Card>
        <h2>Tasks requiring attention</h2>
        {data?.tasks.length ? (
          data.tasks.map((task) => (
            <p key={task.code}>
              <strong>{task.count}</strong> {task.label}
            </p>
          ))
        ) : (
          <p>No outstanding dashboard tasks.</p>
        )}
      </Card>
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
