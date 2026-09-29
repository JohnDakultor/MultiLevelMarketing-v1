"use client";
import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import { AgentStatus } from "@modular-mlm/contracts";
import {
  Alert,
  Button,
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
import { Failure, Loading, date, useAdminScope } from "../shared/AdminState";
import { agentStatus } from "../shared/status";

export function AgentOperationsPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const [message, setMessage] = useState("");
  const [actingAction, setActingAction] = useState<string | null>(null);
  const [selectedAgentId, setSelectedAgentId] = useState<string | null>(null);
  const lifecycle = useFormSubmission();
  const agents = useApiQuery(
    (client, signal) =>
      adminApi.agents(
        client,
        scope.organizationId,
        page,
        deferredSearch,
        signal,
      ),
    [scope.organizationId, page, deferredSearch],
    scope.isReady,
  );
  const applications = useApiQuery(
    (client, signal) =>
      adminApi.applications(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const placementCandidates = useApiQuery(
    (client, signal) =>
      adminApi.placementCandidates(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady && selectedAgentId !== null,
  );
  if (agents.isLoading || applications.isLoading) return <Loading />;
  if (agents.error)
    return <Failure error={agents.error} retry={agents.reload} />;
  const action = async (
    agentId: string,
    code: string,
    value: "approve" | "reject" | "activate" | "suspend" | "reactivate",
  ) => {
    if (
      !(await confirm({
        title: `${value} Agent?`,
        description: `${value} Agent ${code}? This changes the Agent's lifecycle status.`,
        confirmLabel: `${value} Agent`,
      }))
    )
      return;
    setActingAction(`${agentId}:${value}`);
    const completed = await lifecycle.submit(
      () => adminApi.agentAction(api, scope.organizationId, agentId, value),
      `Agent ${code} ${value} request completed.`,
    );
    setActingAction(null);
    if (!completed) return;
    setMessage(`Agent ${code} ${value} request completed.`);
    agents.reload();
    applications.reload();
  };
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Network"
        title="Agent operations"
        description="Review applications and manage lifecycle within the current organization."
      />
      {message && <Alert title={message} tone="success" />}
      <FormErrorSummary
        errors={lifecycle.fieldErrors}
        generalErrors={lifecycle.formErrors}
        id={lifecycle.errorSummaryId}
      />
      <h2>Applications</h2>
      {!applications.data?.items.length ? (
        <EmptyState
          title="No applications"
          description="New Agent applications will appear here."
        />
      ) : (
        <DataTable
          caption="Agent applications"
          rows={applications.data.items}
          rowKey={(row) => row.agentId}
          columns={[
            {
              key: "agent",
              header: "Applicant",
              cell: (row) => (
                <>
                  {row.displayName}
                  <br />
                  <small>{row.email}</small>
                </>
              ),
            },
            { key: "code", header: "Code", cell: (row) => row.agentCode },
            {
              key: "sponsor",
              header: "Sponsor",
              cell: (row) => row.sponsorAgentCode ?? "None",
            },
            {
              key: "date",
              header: "Applied",
              cell: (row) => date(row.joinedAt),
            },
            {
              key: "status",
              header: "Status",
              cell: (row) => <AgentStatusBadge value={row.status} />,
            },
            {
              key: "actions",
              header: "Actions",
              cell: (row) => {
                const canReview = row.status === AgentStatus.pendingApproval;
                if (!canReview) return <span>No actions available</span>;
                return (
                  <>
                    <Button
                      disabled={lifecycle.isSubmitting}
                      isLoading={
                        actingAction === `${row.agentId}:approve` &&
                        lifecycle.isSubmitting
                      }
                      loadingLabel="Saving"
                      onClick={() =>
                        void action(row.agentId, row.agentCode, "approve")
                      }
                    >
                      Approve
                    </Button>{" "}
                    <Button
                      variant="danger"
                      disabled={lifecycle.isSubmitting}
                      isLoading={
                        actingAction === `${row.agentId}:reject` &&
                        lifecycle.isSubmitting
                      }
                      loadingLabel="Saving"
                      onClick={() =>
                        void action(row.agentId, row.agentCode, "reject")
                      }
                    >
                      Reject
                    </Button>
                  </>
                );
              },
            },
          ]}
        />
      )}
      <div className="toolbar">
        <InputField
          id="agent-search"
          label="Search Agents"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
        />
      </div>
      <h2>Agents</h2>
      <DataTable
        caption="Agents"
        rows={agents.data?.items ?? []}
        rowKey={(row) => row.agentId}
        columns={[
          {
            key: "agent",
            header: "Agent",
            cell: (row) => (
              <>
                {row.displayName}
                <br />
                <small>
                  {row.agentCode} · {row.email}
                </small>
              </>
            ),
          },
          {
            key: "status",
            header: "Status",
            cell: (row) => <AgentStatusBadge value={row.status} />,
          },
          {
            key: "sponsor",
            header: "Sponsor",
            cell: (row) => row.sponsorAgentCode ?? "None",
          },
          {
            key: "placement",
            header: "Placement",
            cell: (row) =>
              row.sponsorAgentId === null
                ? "Founding root"
                : row.isPlaced
                  ? `Placed · ${row.placementSide === 0 ? "Left" : "Right"}`
                  : "Unplaced",
          },
          {
            key: "actions",
            header: "Lifecycle",
            cell: (row) => {
              const canActivate = row.status === AgentStatus.inactive;
              const canSuspend = row.status === AgentStatus.active;
              const canReactivate = row.status === AgentStatus.suspended;
              const canPlace =
                row.status === AgentStatus.active &&
                !row.isPlaced &&
                row.sponsorAgentId !== null;
              return (
                <div className="button-cluster">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setSelectedAgentId(row.agentId)}
                  >
                    Details
                  </Button>{" "}
                  {canActivate && (
                    <Button
                      size="sm"
                      variant="secondary"
                      disabled={lifecycle.isSubmitting}
                      isLoading={actingAction === `${row.agentId}:activate`}
                      onClick={() =>
                        void action(row.agentId, row.agentCode, "activate")
                      }
                    >
                      Activate
                    </Button>
                  )}
                  {canSuspend && (
                    <Button
                      size="sm"
                      variant="danger"
                      disabled={lifecycle.isSubmitting}
                      isLoading={actingAction === `${row.agentId}:suspend`}
                      onClick={() =>
                        void action(row.agentId, row.agentCode, "suspend")
                      }
                    >
                      Suspend
                    </Button>
                  )}
                  {canReactivate && (
                    <Button
                      size="sm"
                      variant="secondary"
                      disabled={lifecycle.isSubmitting}
                      isLoading={actingAction === `${row.agentId}:reactivate`}
                      onClick={() =>
                        void action(row.agentId, row.agentCode, "reactivate")
                      }
                    >
                      Reactivate
                    </Button>
                  )}
                  {canPlace && (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={async () => {
                        if (
                          !(await confirm({
                            title: "Automatically place Agent?",
                            description: `Place Agent ${row.agentCode} using the organization's configured placement strategy?`,
                            confirmLabel: "Place Agent",
                          }))
                        )
                          return;
                        const completed = await lifecycle.submit(
                          () =>
                            adminApi.autoPlaceAgent(
                              api,
                              scope.organizationId,
                              row.agentId,
                            ),
                          `Agent ${row.agentCode} placed.`,
                        );
                        if (completed) {
                          setMessage(`Agent ${row.agentCode} placed.`);
                          agents.reload();
                        }
                      }}
                    >
                      Auto-place
                    </Button>
                  )}
                  {row.status === AgentStatus.active && (
                    <WalletAdjustment
                      agentId={row.agentId}
                      agentCode={row.agentCode}
                      onSaved={() => {
                        setMessage(
                          `Wallet adjustment recorded for ${row.agentCode}.`,
                        );
                      }}
                    />
                  )}
                </div>
              );
            },
          },
        ]}
      />
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
          disabled={(agents.data?.items.length ?? 0) < 20}
          onClick={() => setPage((v) => v + 1)}
        >
          Next
        </Button>
      </div>
      {selectedAgentId && (
        <AgentDetails
          agentId={selectedAgentId}
          candidates={(placementCandidates.data?.items ?? []).filter(
            (candidate) => candidate.agentId !== selectedAgentId,
          )}
          candidatesLoading={placementCandidates.isLoading}
          candidatesError={placementCandidates.error}
          retryCandidates={placementCandidates.reload}
          close={() => setSelectedAgentId(null)}
          saved={(value) => {
            setMessage(value);
            agents.reload();
          }}
        />
      )}
    </div>
  );
}

function AgentStatusBadge({ value }: { value: number }) {
  const status = agentStatus(value);
  return <StatusBadge label={status.label} tone={status.tone} />;
}

function AgentDetails({
  agentId,
  candidates,
  candidatesLoading,
  candidatesError,
  retryCandidates,
  close,
  saved,
}: {
  agentId: string;
  candidates: import("@modular-mlm/contracts").AdminAgentSummaryDto[];
  candidatesLoading: boolean;
  candidatesError: Error | null;
  retryCandidates(): void;
  close(): void;
  saved(message: string): void;
}) {
  const api = useApiClient();
  const scope = useAdminScope();
  const { confirm } = useConfirmation();
  const feedback = useFormSubmission();
  const details = useApiQuery(
    (client, signal) =>
      adminApi.agentDetails(client, scope.organizationId, agentId, signal),
    [scope.organizationId, agentId],
    scope.isReady,
  );
  const plans = useApiQuery(
    (client, signal) => adminApi.plans(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  if (details.isLoading || plans.isLoading || candidatesLoading)
    return <Loading />;
  if (details.error)
    return <Failure error={details.error} retry={details.reload} />;
  if (candidatesError)
    return <Failure error={candidatesError} retry={retryCandidates} />;
  if (!details.data)
    return (
      <EmptyState
        title="Agent unavailable"
        description="The Agent no longer exists in this organization."
      />
    );
  const agent = details.data;
  async function submit(
    operation: () => Promise<unknown>,
    title: string,
    description: string,
    success: string,
  ) {
    if (!(await confirm({ title, description, confirmLabel: title }))) return;
    const completed = await feedback.submit(operation, success);
    if (completed) {
      details.reload();
      saved(success);
    }
  }
  return (
    <div className="content-stack">
      <div className="action-row">
        <div>
          <h2>{agent.displayName}</h2>
          <p>
            {agent.agentCode} · {agent.email}
          </p>
          <div className="action-row">
            <AgentStatusBadge value={agent.status} />
            <StatusBadge
              label={`Qualification: ${agent.qualificationState}`}
              tone="neutral"
            />
          </div>
        </div>
        <Button variant="ghost" onClick={close}>
          Close
        </Button>
      </div>
      {agent.sponsorAgentId !== null && (
        <Alert
          title={agent.placement.moveEligibilityReason}
          tone={agent.placement.isMoveEligible ? "info" : "warning"}
        />
      )}
      <FormErrorSummary
        errors={feedback.fieldErrors}
        generalErrors={feedback.formErrors}
        id={feedback.errorSummaryId}
      />
      {agent.sponsorAgentId === null ? (
        <Alert title="Founding network root" tone="info">
          This is the organization&apos;s first Agent. A root has no sponsor or
          placement parent, so no placement action is required.
        </Alert>
      ) : (
        <form
          className="form-grid"
          onSubmit={(event) => {
            event.preventDefault();
            const data = new FormData(event.currentTarget);
            const parentId = String(data.get("parentAgentId"));
            const side = Number(data.get("side"));
            const operation = agent.placement.isPlaced
              ? () =>
                  adminApi.moveAgentPlacement(
                    api,
                    scope.organizationId,
                    agentId,
                    {
                      newParentAgentId: parentId,
                      newSide: side,
                      expectedCurrentParentAgentId:
                        agent.placement.parentAgentId,
                      expectedCurrentSide: agent.placement.side,
                      reason: String(data.get("reason")),
                    },
                  )
              : () =>
                  adminApi.placeAgent(
                    api,
                    scope.organizationId,
                    agentId,
                    parentId,
                    side,
                  );
            void submit(
              operation,
              agent.placement.isPlaced ? "Move placement" : "Place Agent",
              `${agent.placement.isPlaced ? "Move" : "Place"} ${agent.agentCode} under the selected Agent?`,
              "Agent placement updated.",
            );
          }}
        >
          <h3>
            {agent.placement.isPlaced
              ? "Move uncommitted placement"
              : "Manual placement"}
          </h3>
          <SelectField
            name="parentAgentId"
            label="Parent Agent"
            required
            defaultValue={agent.placement.parentAgentId ?? ""}
          >
            <option value="">Select an Agent</option>
            {candidates.map((candidate) => (
              <option key={candidate.agentId} value={candidate.agentId}>
                {candidate.agentCode} · {candidate.displayName}
              </option>
            ))}
          </SelectField>
          <SelectField
            name="side"
            label="Placement side"
            required
            defaultValue={agent.placement.side ?? 0}
          >
            <option value="0">Left</option>
            <option value="1">Right</option>
          </SelectField>
          {agent.placement.isPlaced && (
            <InputField name="reason" label="Move reason" required />
          )}
          <Button
            type="submit"
            disabled={
              agent.placement.isPlaced && !agent.placement.isMoveEligible
            }
            isLoading={feedback.isSubmitting}
          >
            {agent.placement.isPlaced ? "Move placement" : "Place Agent"}
          </Button>
        </form>
      )}
      <form
        className="form-grid"
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const planId = String(data.get("commissionPlanId"));
          const periodStart = new Date(
            String(data.get("periodStart")),
          ).toISOString();
          const periodEnd = new Date(
            String(data.get("periodEnd")),
          ).toISOString();
          void submit(
            () =>
              adminApi.processPairing(api, scope.organizationId, agentId, {
                commissionPlanId: planId,
                periodStart,
                periodEnd,
                idempotencyKey: crypto.randomUUID(),
              }),
            "Run binary pairing",
            `Run pairing for ${agent.agentCode} for the selected period?`,
            "Binary pairing run completed.",
          );
        }}
      >
        <h3>Manual binary-pairing run</h3>
        <SelectField name="commissionPlanId" label="Commission plan" required>
          <option value="">Select a plan</option>
          {plans.data?.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.name} · version {plan.version}
            </option>
          ))}
        </SelectField>
        <InputField
          name="periodStart"
          label="Period start"
          type="datetime-local"
          required
        />
        <InputField
          name="periodEnd"
          label="Period end"
          type="datetime-local"
          required
        />
        <Button
          type="submit"
          variant="secondary"
          isLoading={feedback.isSubmitting}
        >
          Run pairing
        </Button>
      </form>
    </div>
  );
}

function WalletAdjustment({
  agentId,
  agentCode,
  onSaved,
}: {
  agentId: string;
  agentCode: string;
  onSaved(): void;
}) {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const scope = useAdminScope();
  const feedback = useFormSubmission();
  return (
    <details>
      <summary>Wallet adjustment</summary>
      <form
        className="form-grid compact-form"
        onSubmit={async (event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          const amount = Number(data.get("amount"));
          const direction = Number(data.get("direction"));
          const reason = String(data.get("reason"));
          if (
            !(await confirm({
              title: `${direction === 0 ? "Credit" : "Debit"} Agent wallet?`,
              description: `${direction === 0 ? "Credit" : "Debit"} ${scope.currency} ${amount} for Agent ${agentCode}? Reason: ${reason}`,
              confirmLabel: `${direction === 0 ? "Credit" : "Debit"} wallet`,
            }))
          )
            return;
          const idempotencyKey = crypto.randomUUID();
          const form = event.currentTarget;
          const saved = await feedback.submit(
            () =>
              adminApi.adjustWallet(api, scope.organizationId, agentId, {
                organizationId: scope.organizationId,
                agentId,
                direction,
                amount,
                currency: scope.currency,
                reason,
                idempotencyKey,
              }),
            "Wallet adjustment recorded.",
          );
          if (saved) {
            form.reset();
            onSaved();
          }
        }}
      >
        <FormErrorSummary
          errors={feedback.fieldErrors}
          generalErrors={feedback.formErrors}
          id={feedback.errorSummaryId}
        />
        <SelectField
          name="direction"
          label="Direction"
          error={feedback.fieldError("direction")}
        >
          <option value="0">Credit</option>
          <option value="1">Debit</option>
        </SelectField>
        <InputField
          name="amount"
          label={`Amount (${scope.currency})`}
          type="number"
          min="0.01"
          step="0.01"
          required
          error={feedback.fieldError("amount")}
        />
        <InputField
          name="reason"
          label="Reason"
          required
          error={feedback.fieldError("reason")}
        />
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Confirm adjustment
        </Button>
      </form>
    </details>
  );
}
