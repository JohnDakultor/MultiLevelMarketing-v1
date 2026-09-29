"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import {
  Alert,
  Card,
  EmptyState,
  PageHeader,
  SectionHeader,
  StatsCard,
} from "@modular-mlm/design-system";
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
  const currency = data?.currency ?? scope.currency;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Overview"
        title="Operations dashboard"
        description={`Live operational totals from the reporting API${data?.observedAt ? ` · Updated ${new Date(data.observedAt).toLocaleString()}` : ""}.`}
      />
      <div className="metric-grid">
        <StatsCard
          label="Sales today"
          value={money(data?.salesToday ?? 0, currency)}
          description="Gross sales since midnight"
        />
        <StatsCard
          label="Sales · 30 days"
          value={money(data?.salesLast30Days ?? 0, currency)}
          description="Rolling thirty-day total"
        />
        <StatsCard
          label="Orders today"
          value={String(data?.ordersToday ?? 0)}
          description="Orders created today"
        />
        <StatsCard
          label="Active agents"
          value={String(data?.activeAgents ?? 0)}
          description="Currently active network members"
        />
        <StatsCard
          label="Commission liability"
          value={money(data?.commissionLiability ?? 0, currency)}
          tone={(data?.commissionLiability ?? 0) > 0 ? "warning" : "neutral"}
          description="Outstanding commission obligation"
        />
        <StatsCard
          label="Wallet liability"
          value={money(data?.walletLiability ?? 0, currency)}
          tone={(data?.walletLiability ?? 0) > 0 ? "warning" : "neutral"}
          description="Net agent wallet obligation"
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
        <SectionHeader
          title="Tasks requiring attention"
          description="Backend-generated work queues that need an administrator."
        />
        {data?.tasks.length ? (
          <div className="task-list">
            {data.tasks.map((task) => (
              <div className="task-list__item" key={task.code}>
                <span>{task.label}</span>
                <strong>{task.count}</strong>
              </div>
            ))}
          </div>
        ) : (
          <EmptyState
            title="You are all caught up"
            description="There are no outstanding operational tasks."
          />
        )}
      </Card>
    </div>
  );
}
