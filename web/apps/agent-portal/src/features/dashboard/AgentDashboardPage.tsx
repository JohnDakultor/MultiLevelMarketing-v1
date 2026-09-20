"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import { Alert, Card, PageHeader } from "@modular-mlm/design-system";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, money } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";

export function AgentDashboardPage() {
  const scope = useAgentScope();
  const to = new Date();
  const from = new Date(to);
  from.setDate(from.getDate() - 30);
  const report = useApiQuery(
    (api, signal) =>
      agentApi.report(
        api,
        scope.organizationId,
        from.toISOString(),
        to.toISOString(),
        signal,
      ),
    [scope.organizationId],
    scope.isReady,
  );
  const qualification = useApiQuery(
    (api, signal) => agentApi.qualification(api, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const earnings = useApiQuery(
    (api, signal) =>
      agentApi.earnings(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  if (report.isLoading || qualification.isLoading || earnings.isLoading)
    return <Loading />;
  if (report.error)
    return <Failure error={report.error} retry={report.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Last 30 days"
        title="Agent dashboard"
        description="Aggregates are returned by the reporting and compensation APIs."
      />
      {qualification.data && (
        <Alert
          title={
            qualification.data.isQualified
              ? "Qualified"
              : "Qualification needs attention"
          }
          tone={qualification.data.isQualified ? "success" : "warning"}
        >
          {qualification.data.failures
            .map((failure) => failure.message)
            .join(" · ") || qualification.data.state}
        </Alert>
      )}
      <div className="metric-grid">
        <Metric
          label="Attributed sales"
          value={money(
            report.data?.netAttributedSales ?? 0,
            report.data?.currency ?? scope.currency,
          )}
        />
        <Metric
          label="Orders"
          value={String(report.data?.attributedOrders ?? 0)}
        />
        <Metric
          label="Business volume"
          value={String(report.data?.businessVolume ?? 0)}
        />
        <Metric
          label="Direct recruits"
          value={String(report.data?.directRecruits ?? 0)}
        />
        <Metric
          label="Available earnings"
          value={money(earnings.data?.availableAmount ?? 0, scope.currency)}
        />
        <Metric
          label="Pending earnings"
          value={money(earnings.data?.pendingAmount ?? 0, scope.currency)}
        />
      </div>
      <Card>
        <h2>Commission breakdown</h2>
        {report.data?.commissions.length ? (
          report.data.commissions.map((item) => (
            <p key={item.type}>
              Type {item.type}: {item.count} ·{" "}
              {money(item.amount, report.data!.currency)}
            </p>
          ))
        ) : (
          <p>No commissions in this period.</p>
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
