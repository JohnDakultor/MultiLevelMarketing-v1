"use client";
import { useApiClient, useApiQuery } from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  useConfirmation,
} from "@modular-mlm/design-system";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";

export function AuditTrailPage() {
  const scope = useAdminScope();
  const audit = useApiQuery(
    (api, signal) => adminApi.audit(api, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  if (audit.isLoading) return <Loading />;
  if (audit.error) return <Failure error={audit.error} retry={audit.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Audit trail"
        description="Immutable administrative and financial actions for this organization."
      />
      {!audit.data?.length ? (
        <EmptyState
          title="No audit records"
          description="Audited operations will appear here."
        />
      ) : (
        <DataTable
          caption="Audit trail"
          rows={audit.data}
          rowKey={(row) => row.id}
          columns={[
            { key: "time", header: "Time", cell: (row) => date(row.createdAt) },
            { key: "action", header: "Action", cell: (row) => row.action },
            {
              key: "entity",
              header: "Entity",
              cell: (row) => (
                <>
                  {row.entityType}
                  <br />
                  <code>{row.entityId.slice(0, 8)}</code>
                </>
              ),
            },
            {
              key: "reason",
              header: "Reason",
              cell: (row) => row.reason ?? "—",
            },
            {
              key: "trace",
              header: "Trace",
              cell: (row) => row.traceId ?? "—",
            },
          ]}
        />
      )}
    </div>
  );
}
export function OperationalHealthPage() {
  const api = useApiClient();
  const { prompt } = useConfirmation();
  const scope = useAdminScope();
  const health = useApiQuery(
    (client, signal) => adminApi.health(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const dead = useApiQuery(
    (client, signal) =>
      adminApi.deadLetters(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  if (health.isLoading || dead.isLoading) return <Loading />;
  if (health.error)
    return <Failure error={health.error} retry={health.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        title="Operational health"
        description="Provider and background-processing state exposed by the backend."
      />
      <Alert title="Operational snapshot" tone="info">
        Observed {date(health.data?.observedAt ?? null)}
      </Alert>
      <div className="metric-grid">
        <HealthMetric
          label="Pending outbox"
          value={health.data?.pendingOutboxMessages}
        />
        <HealthMetric
          label="Failed webhooks"
          value={health.data?.failedWebhookAttempts}
        />
        <HealthMetric
          label="Payments to reconcile"
          value={health.data?.paymentsAwaitingReconciliation}
        />
        <HealthMetric
          label="Payouts awaiting provider"
          value={health.data?.payoutsAwaitingProviderCompletion}
        />
        <HealthMetric
          label="Compensation backlog"
          value={health.data?.compensationBacklog}
        />
        <HealthMetric
          label="Recent pairing failures"
          value={health.data?.recentPairingFailures}
        />
      </div>
      <h2>Dead-letter messages</h2>
      {!dead.data?.items.length ? (
        <EmptyState
          title="No failed messages"
          description="There are no messages waiting for intervention."
        />
      ) : (
        <DataTable
          caption="Failed messages"
          rows={dead.data.items}
          rowKey={(row) => row.messageId}
          columns={[
            { key: "type", header: "Type", cell: (row) => row.messageType },
            {
              key: "attempts",
              header: "Attempts",
              cell: (row) => row.attempts,
            },
            {
              key: "error",
              header: "Last error",
              cell: (row) => row.lastErrorSummary ?? "No summary",
            },
            {
              key: "occurred",
              header: "Occurred",
              cell: (row) => date(row.occurredAt),
            },
            {
              key: "action",
              header: "",
              cell: (row) => (
                <Button
                  variant="secondary"
                  onClick={async () => {
                    const reason = await prompt({
                      title: "Replay failed message?",
                      description: `Explain why ${row.messageType} should be replayed. The reason is retained in the audit trail.`,
                      label: "Replay reason",
                      submitLabel: "Replay message",
                    });
                    if (!reason) return;
                    await adminApi.replayDeadLetter(
                      api,
                      scope.organizationId,
                      row.messageId,
                      row.attempts,
                      reason,
                    );
                    dead.reload();
                  }}
                >
                  Replay
                </Button>
              ),
            },
          ]}
        />
      )}
    </div>
  );
}

function HealthMetric({ label, value }: { label: string; value?: number }) {
  return (
    <Card>
      <span>{label}</span>
      <strong className="metric-value">{value ?? 0}</strong>
    </Card>
  );
}
