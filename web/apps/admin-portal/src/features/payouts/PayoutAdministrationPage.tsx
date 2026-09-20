"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { adminApi } from "../api/adminApi";
import {
  Failure,
  Loading,
  date,
  money,
  useAdminScope,
} from "../shared/AdminState";

export function PayoutAdministrationPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const [message, setMessage] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const feedback = useFormSubmission();
  const payouts = useApiQuery(
    (client, signal) =>
      adminApi.payouts(client, scope.organizationId, page, signal),
    [scope.organizationId, page],
    scope.isReady,
  );
  if (payouts.isLoading) return <Loading />;
  if (payouts.error)
    return <Failure error={payouts.error} retry={payouts.reload} />;
  const action = async (
    id: string,
    value: "approve" | "reject" | "process" | "reconcile",
  ) => {
    if (
      !(await confirm({
        title: `${value} payout request?`,
        description: `${value} payout ${id}? This financial operation is recorded in the audit trail.`,
        confirmLabel: `${value} payout`,
      }))
    )
      return;
    const completed = await feedback.submit(
      () => adminApi.payoutAction(api, scope.organizationId, id, value),
      `Payout ${value} completed.`,
    );
    if (completed) {
      setMessage(`Payout ${id} ${value} completed.`);
      payouts.reload();
    }
  };
  return (
    <div className="content-stack">
      <PageHeader
        title="Payout administration"
        description="Review and advance provider-backed payout requests with explicit confirmation."
      />
      {message && <Alert title={message} tone="info" />}
      {!payouts.data?.length ? (
        <EmptyState
          title="No payout requests"
          description="Agent payout requests will appear here."
        />
      ) : (
        <DataTable
          caption="Payout requests"
          rows={payouts.data}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "id",
              header: "Request",
              cell: (row) => <code>{row.id.slice(0, 8)}</code>,
            },
            {
              key: "date",
              header: "Requested",
              cell: (row) => date(row.requestedAt),
            },
            {
              key: "amount",
              header: "Amount",
              cell: (row) => money(row.amount, row.currency),
            },
            { key: "status", header: "Status", cell: (row) => row.status },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => (
                <div className="button-cluster">
                  <Button onClick={() => void action(row.id, "approve")}>
                    Approve
                  </Button>
                  <Button
                    variant="danger"
                    onClick={() => void action(row.id, "reject")}
                  >
                    Reject
                  </Button>
                  <Button
                    variant="secondary"
                    onClick={() => void action(row.id, "process")}
                  >
                    Submit
                  </Button>
                  <Button
                    variant="ghost"
                    onClick={() => void action(row.id, "reconcile")}
                  >
                    Reconcile
                  </Button>
                  <Button variant="ghost" onClick={() => setSelectedId(row.id)}>
                    Details
                  </Button>
                </div>
              ),
            },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={page === 1}
          onClick={() => setPage((v) => v - 1)}
        >
          Previous
        </Button>
        <span>Page {page}</span>
        <Button
          variant="secondary"
          disabled={(payouts.data?.length ?? 0) < 20}
          onClick={() => setPage((v) => v + 1)}
        >
          Next
        </Button>
      </div>
      {selectedId && (
        <PayoutDetails
          payoutId={selectedId}
          close={() => setSelectedId(null)}
          saved={(value) => {
            setMessage(value);
            payouts.reload();
          }}
        />
      )}
    </div>
  );
}

function PayoutDetails({
  payoutId,
  close,
  saved,
}: {
  payoutId: string;
  close(): void;
  saved(message: string): void;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  const { confirm } = useConfirmation();
  const details = useApiQuery(
    (client, signal) =>
      adminApi.payoutDetails(client, scope.organizationId, payoutId, signal),
    [scope.organizationId, payoutId],
    scope.isReady,
  );
  if (details.isLoading) return <Loading />;
  if (details.error)
    return <Failure error={details.error} retry={details.reload} />;
  if (!details.data)
    return (
      <EmptyState
        title="Payout unavailable"
        description="This payout no longer exists in the organization."
      />
    );
  const payout = details.data;
  async function accountAction(action: "verify" | "reject") {
    if (
      !(await confirm({
        title: `${action} payout account?`,
        description: `${action} the account associated with this ${money(payout.amount, payout.currency)} payout? This changes whether the account can receive payouts.`,
        confirmLabel: `${action} account`,
      }))
    )
      return;
    try {
      await adminApi.payoutAccountAction(
        api,
        scope.organizationId,
        payout.payoutAccountId,
        action,
      );
      saved(`Payout account ${action} completed.`);
      details.reload();
    } catch (error) {
      saved(
        error instanceof Error
          ? error.message
          : "Payout-account operation failed.",
      );
    }
  }
  return (
    <Card>
      <div className="action-row">
        <div>
          <h2>Payout details</h2>
          <p>
            {money(payout.amount, payout.currency)} · requested{" "}
            {date(payout.requestedAt)}
          </p>
        </div>
        <Button variant="ghost" onClick={close}>
          Close
        </Button>
      </div>
      <dl>
        <dt>Status</dt>
        <dd>{payout.status}</dd>
        <dt>Provider reference</dt>
        <dd>{payout.providerReference ?? "Not assigned"}</dd>
        <dt>Transfer</dt>
        <dd>{payout.providerTransferId ?? "Not submitted"}</dd>
        <dt>Failure</dt>
        <dd>{payout.failureMessage ?? "None"}</dd>
      </dl>
      <div className="button-cluster">
        <Button onClick={() => void accountAction("verify")}>
          Verify payout account
        </Button>
        <Button variant="danger" onClick={() => void accountAction("reject")}>
          Reject payout account
        </Button>
      </div>
      <p>
        <small>
          The backend does not currently accept a rejection reason for payout
          accounts, so this confirmation cannot persist one.
        </small>
      </p>
    </Card>
  );
}
