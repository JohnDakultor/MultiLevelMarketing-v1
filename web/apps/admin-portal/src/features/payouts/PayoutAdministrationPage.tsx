"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import { PayoutStatus, PayoutVerificationStatus } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  StatusBadge,
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
import { payoutAccountStatus, payoutStatus } from "../shared/status";

export function PayoutAdministrationPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const [accountPage, setAccountPage] = useState(1);
  const [message, setMessage] = useState("");
  const [actingAction, setActingAction] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const feedback = useFormSubmission();
  const payouts = useApiQuery(
    (client, signal) =>
      adminApi.payouts(client, scope.organizationId, page, signal),
    [scope.organizationId, page],
    scope.isReady,
  );
  const payoutAccounts = useApiQuery(
    (client, signal) =>
      adminApi.payoutAccounts(
        client,
        scope.organizationId,
        accountPage,
        signal,
        "Pending",
      ),
    [scope.organizationId, accountPage],
    scope.isReady,
  );
  if (payouts.isLoading || payoutAccounts.isLoading) return <Loading />;
  if (payouts.error)
    return <Failure error={payouts.error} retry={payouts.reload} />;
  if (payoutAccounts.error)
    return (
      <Failure error={payoutAccounts.error} retry={payoutAccounts.reload} />
    );
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
    setActingAction(`${id}:${value}`);
    const completed = await feedback.submit(
      () => adminApi.payoutAction(api, scope.organizationId, id, value),
      `Payout ${value} completed.`,
    );
    setActingAction(null);
    if (completed) {
      setMessage(`Payout ${id} ${value} completed.`);
      payouts.reload();
    }
  };
  const reviewAccount = async (id: string, value: "verify" | "reject") => {
    if (
      !(await confirm({
        title: `${value} payout account?`,
        description:
          value === "verify"
            ? "Confirm that the masked destination details have completed your organization's verification process."
            : "Reject this payout destination. The Agent will not be able to request a payout to it.",
        confirmLabel: value === "verify" ? "Verify account" : "Reject account",
      }))
    )
      return;
    setActingAction(`${id}:account:${value}`);
    const completed = await feedback.submit(
      () => adminApi.payoutAccountAction(api, scope.organizationId, id, value),
      `Payout account ${value === "verify" ? "verified" : "rejected"}.`,
    );
    setActingAction(null);
    if (completed) {
      setMessage(
        `Payout account ${value === "verify" ? "verified" : "rejected"}.`,
      );
      payoutAccounts.reload();
    }
  };
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Finance"
        title="Payout administration"
        description="Review and advance provider-backed payout requests with explicit confirmation."
      />
      {message && <Alert title={message} tone="info" />}
      <Card>
        <div className="action-row">
          <div>
            <h2>Payout accounts awaiting verification</h2>
            <p>
              Review masked bank or e-wallet destinations submitted by Agents.
            </p>
          </div>
          <StatusBadge
            label={`${payoutAccounts.data?.totalCount ?? 0} pending`}
            tone={
              (payoutAccounts.data?.totalCount ?? 0) > 0 ? "warning" : "neutral"
            }
          />
        </div>
        {!payoutAccounts.data?.items.length ? (
          <EmptyState
            title="No payout accounts awaiting verification"
            description="Submitted Agent payout destinations will appear here for review."
          />
        ) : (
          <DataTable
            caption="Payout accounts awaiting verification"
            rows={payoutAccounts.data.items}
            rowKey={(row) => row.id}
            columns={[
              {
                key: "agent",
                header: "Agent",
                cell: (row) => row.agentCode,
              },
              {
                key: "destination",
                header: "Destination",
                cell: (row) => `${row.method} · ${row.maskedAccountData}`,
              },
              {
                key: "network",
                header: "Bank / rail",
                cell: (row) => `${row.bankCode} · ${row.rail}`,
              },
              {
                key: "submitted",
                header: "Submitted",
                cell: (row) => date(row.createdAt),
              },
              {
                key: "verification",
                header: "Status",
                cell: (row) => {
                  const status = payoutAccountStatus(row.verificationStatus);
                  return (
                    <StatusBadge label={status.label} tone={status.tone} />
                  );
                },
              },
              {
                key: "actions",
                header: "Actions",
                cell: (row) =>
                  row.verificationStatus ===
                  PayoutVerificationStatus.pending ? (
                    <div className="button-cluster">
                      <Button
                        size="sm"
                        disabled={feedback.isSubmitting}
                        isLoading={actingAction === `${row.id}:account:verify`}
                        onClick={() => void reviewAccount(row.id, "verify")}
                      >
                        Verify
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        disabled={feedback.isSubmitting}
                        isLoading={actingAction === `${row.id}:account:reject`}
                        onClick={() => void reviewAccount(row.id, "reject")}
                      >
                        Reject
                      </Button>
                    </div>
                  ) : null,
              },
            ]}
          />
        )}
        <div className="pagination-row">
          <Button
            variant="secondary"
            disabled={!payoutAccounts.data?.hasPreviousPage}
            onClick={() => setAccountPage((value) => value - 1)}
          >
            Previous
          </Button>
          <span>Page {accountPage}</span>
          <Button
            variant="secondary"
            disabled={!payoutAccounts.data?.hasNextPage}
            onClick={() => setAccountPage((value) => value + 1)}
          >
            Next
          </Button>
        </div>
      </Card>
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
            {
              key: "status",
              header: "Status",
              cell: (row) => <PayoutStatusBadge value={row.status} />,
            },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => {
                const isUnderReview = row.status === PayoutStatus.underReview;
                const isApproved = row.status === PayoutStatus.approved;
                const isProcessing = row.status === PayoutStatus.processing;
                return (
                  <div className="button-cluster">
                    {isUnderReview && (
                      <>
                        <Button
                          size="sm"
                          disabled={feedback.isSubmitting}
                          isLoading={actingAction === `${row.id}:approve`}
                          onClick={() => void action(row.id, "approve")}
                        >
                          Approve
                        </Button>
                        <Button
                          size="sm"
                          variant="danger"
                          disabled={feedback.isSubmitting}
                          isLoading={actingAction === `${row.id}:reject`}
                          onClick={() => void action(row.id, "reject")}
                        >
                          Reject
                        </Button>
                      </>
                    )}
                    {isApproved && (
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={feedback.isSubmitting}
                        isLoading={actingAction === `${row.id}:process`}
                        onClick={() => void action(row.id, "process")}
                      >
                        Submit to provider
                      </Button>
                    )}
                    {isProcessing && (
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={feedback.isSubmitting}
                        isLoading={actingAction === `${row.id}:reconcile`}
                        onClick={() => void action(row.id, "reconcile")}
                      >
                        Reconcile
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      onClick={() => setSelectedId(row.id)}
                    >
                      Details
                    </Button>
                  </div>
                );
              },
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
        />
      )}
    </div>
  );
}

function PayoutDetails({
  payoutId,
  close,
}: {
  payoutId: string;
  close(): void;
}) {
  const scope = useAdminScope();
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
        <dd>
          <PayoutStatusBadge value={payout.status} />
        </dd>
        <dt>Provider reference</dt>
        <dd>{payout.providerReference ?? "Not assigned"}</dd>
        <dt>Transfer</dt>
        <dd>{payout.providerTransferId ?? "Not submitted"}</dd>
        <dt>Failure</dt>
        <dd>{payout.failureMessage ?? "None"}</dd>
      </dl>
    </Card>
  );
}

function PayoutStatusBadge({ value }: { value: number }) {
  const status = payoutStatus(value);
  return <StatusBadge label={status.label} tone={status.tone} />;
}
