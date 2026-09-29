"use client";

import {
  useApiClient,
  useApiQuery,
  useFormSubmission,
} from "@modular-mlm/api-client";
import {
  BinaryVolumeEntryType,
  PayoutStatus,
  PayoutVerificationStatus,
} from "@modular-mlm/contracts";
import {
  Alert,
  Button,
  Card,
  DataTable,
  Dialog,
  EmptyState,
  FormErrorSummary,
  InputField,
  PageHeader,
  SelectField,
  StatsCard,
  useConfirmation,
} from "@modular-mlm/design-system";
import { useState, type FormEvent } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date, money } from "../shared/AgentScreenState";
import { useAgentScope } from "../shared/useAgentScope";
import {
  CommissionStatusBadge,
  PayoutAccountStatusBadge,
  PayoutStatusBadge,
  binaryVolumeEntryTypeLabel,
  commissionTypeLabel,
  walletEntryTypeLabel,
} from "../shared/status";

export function EarningsPage() {
  const scope = useAgentScope();
  const [page, setPage] = useState(1);
  const [volumePage, setVolumePage] = useState(1);
  const [volumeSide, setVolumeSide] = useState("");
  const [volumeType, setVolumeType] = useState("");
  const [volumeFrom, setVolumeFrom] = useState("");
  const [volumeTo, setVolumeTo] = useState("");
  const [pairingFrom, setPairingFrom] = useState(() => dateInputDaysAgo(90));
  const [pairingTo, setPairingTo] = useState(() => dateInputDaysAgo(0));
  const [selectedCommissionId, setSelectedCommissionId] = useState<
    string | null
  >(null);
  const earnings = useApiQuery(
    (api, signal) =>
      agentApi.earnings(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const volume = useApiQuery(
    (api, signal) =>
      agentApi.binaryVolume(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const commissions = useApiQuery(
    (api, signal) =>
      agentApi.commissions(
        api,
        scope.organizationId,
        scope.agentId,
        page,
        signal,
      ),
    [scope.organizationId, scope.agentId, page],
    scope.isReady,
  );
  const volumeLedger = useApiQuery(
    (api, signal) =>
      agentApi.binaryVolumeLedger(
        api,
        scope.organizationId,
        scope.agentId,
        volumePage,
        {
          side: volumeSide === "" ? undefined : Number(volumeSide),
          entryType: volumeType === "" ? undefined : Number(volumeType),
          from: volumeFrom ? startOfDay(volumeFrom) : undefined,
          to: volumeTo ? endOfDay(volumeTo) : undefined,
        },
        signal,
      ),
    [
      scope.organizationId,
      scope.agentId,
      volumePage,
      volumeSide,
      volumeType,
      volumeFrom,
      volumeTo,
    ],
    scope.isReady,
  );
  const pairing = useApiQuery(
    (api, signal) =>
      agentApi.pairingHistory(
        api,
        scope.organizationId,
        scope.agentId,
        {
          periodStart: pairingFrom ? startOfDay(pairingFrom) : undefined,
          periodEnd: pairingTo ? endOfDay(pairingTo) : undefined,
        },
        signal,
      ),
    [scope.organizationId, scope.agentId, pairingFrom, pairingTo],
    scope.isReady,
  );
  const commissionDetails = useApiQuery(
    (api, signal) =>
      agentApi.commission(
        api,
        scope.organizationId,
        scope.agentId,
        selectedCommissionId ?? "",
        signal,
      ),
    [scope.organizationId, scope.agentId, selectedCommissionId],
    scope.isReady && selectedCommissionId !== null,
  );
  if (
    earnings.isLoading ||
    volume.isLoading ||
    commissions.isLoading ||
    volumeLedger.isLoading ||
    pairing.isLoading
  )
    return <Loading />;
  const loadError =
    earnings.error ??
    volume.error ??
    commissions.error ??
    volumeLedger.error ??
    pairing.error;
  if (loadError)
    return (
      <Failure
        error={loadError}
        retry={() => {
          earnings.reload();
          volume.reload();
          commissions.reload();
          volumeLedger.reload();
          pairing.reload();
        }}
      />
    );
  return (
    <div className="content-stack">
      <PageHeader
        title="Earnings and commissions"
        description="Summary totals are independent aggregates; history is a paged activity view."
      />
      <div className="metric-grid">
        <StatsCard
          label="Pending"
          value={money(earnings.data?.pendingAmount ?? 0, scope.currency)}
        />
        <StatsCard
          label="Available"
          value={money(earnings.data?.availableAmount ?? 0, scope.currency)}
        />
        <StatsCard
          label="Paid lifetime"
          value={money(earnings.data?.paidAmount ?? 0, scope.currency)}
        />
        <StatsCard
          label="Direct sales lifetime"
          value={money(earnings.data?.directSalesLifetime ?? 0, scope.currency)}
        />
        <StatsCard
          label="Left available BV"
          value={String(volume.data?.leftAvailable ?? 0)}
        />
        <StatsCard
          label="Right available BV"
          value={String(volume.data?.rightAvailable ?? 0)}
        />
      </div>
      <h2>Binary volume ledger</h2>
      <section className="filter-panel" aria-label="Binary volume filters">
        <SelectField
          id="volume-side"
          label="Side"
          value={volumeSide}
          onChange={(event) => {
            setVolumeSide(event.target.value);
            setVolumePage(1);
          }}
        >
          <option value="">Both sides</option>
          <option value="0">Left</option>
          <option value="1">Right</option>
        </SelectField>
        <SelectField
          id="volume-type"
          label="Entry type"
          value={volumeType}
          onChange={(event) => {
            setVolumeType(event.target.value);
            setVolumePage(1);
          }}
        >
          <option value="">All types</option>
          {Object.values(BinaryVolumeEntryType).map((value) => (
            <option key={value} value={value}>
              {binaryVolumeEntryTypeLabel(value)}
            </option>
          ))}
        </SelectField>
        <InputField
          id="volume-from"
          label="From"
          type="date"
          value={volumeFrom}
          onChange={(event) => {
            setVolumeFrom(event.target.value);
            setVolumePage(1);
          }}
        />
        <InputField
          id="volume-to"
          label="To"
          type="date"
          min={volumeFrom || undefined}
          value={volumeTo}
          onChange={(event) => {
            setVolumeTo(event.target.value);
            setVolumePage(1);
          }}
        />
      </section>
      {volumeLedger.error ? (
        <Failure error={volumeLedger.error} retry={volumeLedger.reload} />
      ) : !volumeLedger.data?.items.length ? (
        <EmptyState
          title="No binary volume entries"
          description="Order and pairing volume entries will appear here."
        />
      ) : (
        <DataTable
          caption="Binary volume ledger"
          rows={volumeLedger.data.items}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "date",
              header: "Effective",
              cell: (row) => date(row.effectiveAt),
            },
            {
              key: "side",
              header: "Side",
              cell: (row) => (row.side === 0 ? "Left" : "Right"),
            },
            {
              key: "type",
              header: "Type",
              cell: (row) => binaryVolumeEntryTypeLabel(row.entryType),
            },
            {
              key: "source",
              header: "Source",
              cell: (row) => row.sourceOrderItemId ?? row.pairingRunId ?? "—",
            },
            { key: "volume", header: "Volume", cell: (row) => row.volume },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!volumeLedger.data?.hasPreviousPage}
          onClick={() => setVolumePage((value) => value - 1)}
        >
          Previous BV
        </Button>
        <span>BV page {volumeLedger.data?.page ?? volumePage}</span>
        <Button
          variant="secondary"
          disabled={!volumeLedger.data?.hasNextPage}
          onClick={() => setVolumePage((value) => value + 1)}
        >
          Next BV
        </Button>
      </div>
      {!commissions.data?.length ? (
        <EmptyState
          title="No commissions"
          description="Commission entries will appear after eligible activity is processed."
        />
      ) : (
        <DataTable
          caption="Commission history"
          rows={commissions.data}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "date",
              header: "Created",
              cell: (row) => date(row.created),
            },
            {
              key: "type",
              header: "Type",
              cell: (row) => commissionTypeLabel(row.type),
            },
            {
              key: "base",
              header: "Base",
              cell: (row) => money(row.baseAmount, scope.currency),
            },
            {
              key: "amount",
              header: "Amount",
              align: "end",
              cell: (row) => money(row.amount, scope.currency),
            },
            {
              key: "status",
              header: "Status",
              cell: (row) => <CommissionStatusBadge value={row.status} />,
            },
            {
              key: "details",
              header: "",
              cell: (row) => (
                <Button
                  variant="secondary"
                  onClick={() => setSelectedCommissionId(row.id)}
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
          disabled={page === 1}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {page}</span>
        <Button
          variant="secondary"
          disabled={(commissions.data?.length ?? 0) < 20}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
      <h2>Binary pairing history</h2>
      <section className="filter-panel" aria-label="Pairing period filters">
        <InputField
          id="pairing-from"
          label="Period start"
          type="date"
          value={pairingFrom}
          onChange={(event) => setPairingFrom(event.target.value)}
        />
        <InputField
          id="pairing-to"
          label="Period end"
          type="date"
          min={pairingFrom}
          value={pairingTo}
          onChange={(event) => setPairingTo(event.target.value)}
        />
      </section>
      {!pairing.data?.length ? (
        <EmptyState
          title="No pairing runs"
          description="Completed binary pairing periods will appear here."
        />
      ) : (
        <DataTable
          caption="Binary pairing history"
          rows={pairing.data}
          rowKey={(row) => `${row.periodStart}-${row.commissionPlanVersion}`}
          columns={[
            {
              key: "period",
              header: "Period",
              cell: (row) =>
                `${date(row.periodStart)} – ${date(row.periodEnd)}`,
            },
            {
              key: "matched",
              header: "Matched BV",
              cell: (row) => row.matchedVolume,
            },
            {
              key: "carry",
              header: "Remaining",
              cell: (row) => `${row.leftAfter} L / ${row.rightAfter} R`,
            },
            {
              key: "qualification",
              header: "Qualification",
              cell: (row) =>
                row.qualificationPassed
                  ? "Passed"
                  : (row.qualificationFailureReason ?? "Failed"),
            },
            {
              key: "net",
              header: "Net commission",
              align: "end",
              cell: (row) => money(row.netCommission, scope.currency),
            },
          ]}
        />
      )}
      <Dialog
        isOpen={selectedCommissionId !== null}
        title="Commission details"
        description="Source and calculation references for this commission entry."
        onClose={() => setSelectedCommissionId(null)}
      >
        {commissionDetails.isLoading ? (
          <Loading />
        ) : commissionDetails.error ? (
          <Failure
            error={commissionDetails.error}
            retry={commissionDetails.reload}
          />
        ) : commissionDetails.data ? (
          <dl className="details-list">
            <div>
              <dt>Created</dt>
              <dd>{date(commissionDetails.data.created)}</dd>
            </div>
            <div>
              <dt>Type</dt>
              <dd>{commissionTypeLabel(commissionDetails.data.type)}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>
                <CommissionStatusBadge value={commissionDetails.data.status} />
              </dd>
            </div>
            <div>
              <dt>Base amount</dt>
              <dd>
                {money(commissionDetails.data.baseAmount, scope.currency)}
              </dd>
            </div>
            <div>
              <dt>Rate</dt>
              <dd>
                {commissionDetails.data.rate === null
                  ? "Not applicable"
                  : `${commissionDetails.data.rate * 100}%`}
              </dd>
            </div>
            <div>
              <dt>Commission amount</dt>
              <dd>{money(commissionDetails.data.amount, scope.currency)}</dd>
            </div>
            <div>
              <dt>Source</dt>
              <dd>
                {commissionDetails.data.sourceOrderId
                  ? `Order ${commissionDetails.data.sourceOrderId}`
                  : commissionDetails.data.pairingRunId
                    ? `Pairing run ${commissionDetails.data.pairingRunId}`
                    : "No source reference"}
              </dd>
            </div>
          </dl>
        ) : null}
      </Dialog>
    </div>
  );
}

export function WalletPage() {
  const scope = useAgentScope();
  const [page, setPage] = useState(1);
  const wallet = useApiQuery(
    (api, signal) =>
      agentApi.wallet(api, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const entries = useApiQuery(
    (api, signal) =>
      agentApi.walletEntries(
        api,
        scope.organizationId,
        scope.agentId,
        page,
        {},
        signal,
      ),
    [scope.organizationId, scope.agentId, page],
    scope.isReady,
  );
  if (wallet.isLoading || entries.isLoading) return <Loading />;
  const loadError = wallet.error ?? entries.error;
  if (loadError)
    return (
      <Failure
        error={loadError}
        retry={() => {
          wallet.reload();
          entries.reload();
        }}
      />
    );
  return (
    <div className="content-stack">
      <PageHeader
        title="Wallet"
        description="Wallet entries are immutable. Balances are calculated by the backend ledger."
      />
      <div className="metric-grid">
        <StatsCard
          label="Available"
          value={money(
            wallet.data?.available ?? 0,
            wallet.data?.currency ?? scope.currency,
          )}
        />
        <StatsCard
          label="Pending"
          value={money(
            wallet.data?.pending ?? 0,
            wallet.data?.currency ?? scope.currency,
          )}
        />
        <StatsCard
          label="Held"
          value={money(
            wallet.data?.held ?? 0,
            wallet.data?.currency ?? scope.currency,
          )}
        />
        <StatsCard
          label="Net"
          value={money(
            wallet.data?.net ?? 0,
            wallet.data?.currency ?? scope.currency,
          )}
        />
      </div>
      {!entries.data?.items.length ? (
        <EmptyState
          title="No wallet activity"
          description="Released commissions and payouts will create ledger entries."
        />
      ) : (
        <DataTable
          caption="Wallet ledger"
          rows={entries.data.items}
          rowKey={(row) => row.id}
          columns={[
            {
              key: "date",
              header: "Created",
              cell: (row) => date(row.createdAt),
            },
            {
              key: "type",
              header: "Type",
              cell: (row) => walletEntryTypeLabel(row.type),
            },
            { key: "source", header: "Source", cell: (row) => row.sourceType },
            {
              key: "available",
              header: "Available at",
              cell: (row) => date(row.availableAt),
            },
            {
              key: "amount",
              header: "Amount",
              align: "end",
              cell: (row) =>
                money(row.amount, wallet.data?.currency ?? scope.currency),
            },
          ]}
        />
      )}
      <div className="pagination-row">
        <Button
          variant="secondary"
          disabled={!entries.data?.hasPreviousPage}
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {entries.data?.page ?? page}</span>
        <Button
          variant="secondary"
          disabled={!entries.data?.hasNextPage}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
    </div>
  );
}

export function PayoutsPage() {
  const api = useApiClient();
  const { confirm } = useConfirmation();
  const payoutFeedback = useFormSubmission();
  const accountActionFeedback = useFormSubmission();
  const cancellationFeedback = useFormSubmission();
  const scope = useAgentScope();
  const [page, setPage] = useState(1);
  const [selectedPayoutId, setSelectedPayoutId] = useState<string | null>(null);
  const accounts = useApiQuery(
    (client, signal) =>
      agentApi.payoutAccounts(
        client,
        scope.organizationId,
        scope.agentId,
        signal,
      ),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const payouts = useApiQuery(
    (client, signal) =>
      agentApi.payouts(
        client,
        scope.organizationId,
        scope.agentId,
        page,
        signal,
      ),
    [scope.organizationId, scope.agentId, page],
    scope.isReady,
  );
  const wallet = useApiQuery(
    (client, signal) =>
      agentApi.wallet(client, scope.organizationId, scope.agentId, signal),
    [scope.organizationId, scope.agentId],
    scope.isReady,
  );
  const context = useApiQuery(
    (client, signal) => agentApi.context(client, scope.organizationId, signal),
    [scope.organizationId],
    scope.isReady,
  );
  const payoutDetails = useApiQuery(
    (client, signal) =>
      agentApi.payout(
        client,
        scope.organizationId,
        scope.agentId,
        selectedPayoutId ?? "",
        signal,
      ),
    [scope.organizationId, scope.agentId, selectedPayoutId],
    scope.isReady && selectedPayoutId !== null,
  );
  const [message, setMessage] = useState("");
  if (
    accounts.isLoading ||
    payouts.isLoading ||
    wallet.isLoading ||
    context.isLoading
  )
    return <Loading />;
  const loadError =
    accounts.error ?? payouts.error ?? wallet.error ?? context.error;
  if (loadError)
    return (
      <Failure
        error={loadError}
        retry={() => {
          accounts.reload();
          payouts.reload();
          wallet.reload();
          context.reload();
        }}
      />
    );
  async function request(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!context.data?.canRequestPayout) return;
    const data = new FormData(event.currentTarget);
    const requested = await payoutFeedback.submit(async () => {
      await agentApi.requestPayout(
        api,
        scope.organizationId,
        scope.agentId,
        String(data.get("accountId")),
        Number(data.get("amount")),
        wallet.data?.currency ?? scope.currency,
      );
    }, "Payout requested.");
    if (requested) {
      setMessage("Payout requested.");
      payouts.reload();
      wallet.reload();
    }
  }
  return (
    <div className="content-stack">
      <PageHeader
        title="Payouts"
        description="Only verified payout accounts and server-confirmed available wallet value can be used."
      />
      {message && <Alert title={message} tone="info" />}
      {!!accountActionFeedback.formErrors.length && (
        <Alert title="Payout account action failed" tone="danger">
          {accountActionFeedback.formErrors.join(" ")}
        </Alert>
      )}
      {!!cancellationFeedback.formErrors.length && (
        <Alert title="Payout cancellation failed" tone="danger">
          {cancellationFeedback.formErrors.join(" ")}
        </Alert>
      )}
      {!context.data?.canRequestPayout && (
        <Alert title="Payout requests are currently unavailable" tone="warning">
          Your Agent status, wallet eligibility, or organization settings do not
          currently permit a payout request.
        </Alert>
      )}
      <div className="detail-grid">
        <Card>
          <h2>Request payout</h2>
          <form onSubmit={request} className="form-grid">
            <FormErrorSummary
              errors={payoutFeedback.fieldErrors}
              generalErrors={payoutFeedback.formErrors}
              id={payoutFeedback.errorSummaryId}
            />
            <SelectField
              name="accountId"
              label="Payout account"
              required
              error={
                payoutFeedback.fieldError("payoutAccountId") ??
                payoutFeedback.fieldError("accountId")
              }
            >
              <option value="">Select verified account</option>
              {accounts.data?.map((account) => (
                <option
                  key={account.id}
                  value={account.id}
                  disabled={account.verificationStatus !== 2}
                >
                  {account.method} · {account.maskedAccountData}
                  {account.isDefault ? " (Default)" : ""}
                </option>
              ))}
            </SelectField>
            <InputField
              name="amount"
              label={`Amount (${wallet.data?.currency ?? scope.currency})`}
              type="number"
              min="0.01"
              step="0.01"
              max={wallet.data?.available}
              required
              error={payoutFeedback.fieldError("amount")}
            />
            <Button
              type="submit"
              isLoading={payoutFeedback.isSubmitting}
              disabled={
                payoutFeedback.isSubmitting || !context.data?.canRequestPayout
              }
            >
              {context.data?.canRequestPayout
                ? "Request payout"
                : "Payout unavailable"}
            </Button>
          </form>
        </Card>
        <PayoutAccountForm
          onSaved={() => {
            setMessage("Payout account registered.");
            accounts.reload();
          }}
        />
      </div>
      <h2>Accounts</h2>
      {!accounts.data?.length ? (
        <EmptyState
          title="No payout accounts"
          description="Register an account before requesting a payout."
        />
      ) : (
        accounts.data.map((account) => (
          <Card key={account.id} className="action-row">
            <div>
              <strong>
                {account.method} · {account.maskedAccountData}
              </strong>
              <p>
                {account.bankCode} · {account.rail} · Verification{" "}
                <PayoutAccountStatusBadge value={account.verificationStatus} />
              </p>
            </div>
            <div>
              {account.verificationStatus ===
                PayoutVerificationStatus.unverified && (
                <Button
                  variant="secondary"
                  isLoading={accountActionFeedback.isSubmitting}
                  disabled={accountActionFeedback.isSubmitting}
                  onClick={async () => {
                    const saved = await accountActionFeedback.submit(
                      () =>
                        agentApi.submitPayoutAccount(
                          api,
                          scope.organizationId,
                          scope.agentId,
                          account.id,
                        ),
                      "Payout account submitted for verification.",
                    );
                    if (saved) {
                      setMessage("Payout account submitted for verification.");
                      accounts.reload();
                    }
                  }}
                >
                  Submit for verification
                </Button>
              )}{" "}
              {!account.isDefault &&
                account.verificationStatus ===
                  PayoutVerificationStatus.verified && (
                  <Button
                    variant="ghost"
                    isLoading={accountActionFeedback.isSubmitting}
                    disabled={accountActionFeedback.isSubmitting}
                    onClick={async () => {
                      const saved = await accountActionFeedback.submit(
                        () =>
                          agentApi.makeDefaultPayoutAccount(
                            api,
                            scope.organizationId,
                            scope.agentId,
                            account.id,
                          ),
                        "Default payout account updated.",
                      );
                      if (saved) {
                        setMessage("Default payout account updated.");
                        accounts.reload();
                      }
                    }}
                  >
                    Make default
                  </Button>
                )}
            </div>
          </Card>
        ))
      )}
      <h2>History</h2>
      {!payouts.data?.length ? (
        <EmptyState
          title="No payouts"
          description="Approved payout requests will appear here."
        />
      ) : (
        <DataTable
          caption="Payout history"
          rows={payouts.data}
          rowKey={(row) => row.id}
          columns={[
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
              key: "reference",
              header: "Reference",
              cell: (row) => row.providerReference ?? "—",
            },
            {
              key: "action",
              header: "",
              cell: (row) => (
                <div className="action-row">
                  <Button
                    variant="secondary"
                    onClick={() => setSelectedPayoutId(row.id)}
                  >
                    View details
                  </Button>
                  {row.status === PayoutStatus.requested ||
                  row.status === PayoutStatus.underReview ||
                  row.status === PayoutStatus.approved ? (
                    <Button
                      variant="danger"
                      onClick={async () => {
                        if (
                          !(await confirm({
                            title: "Cancel payout request?",
                            description: `Cancel payout ${row.id}? A payout already submitted to the provider may no longer be cancellable.`,
                            confirmLabel: "Cancel payout",
                          }))
                        )
                          return;
                        const cancelled = await cancellationFeedback.submit(
                          () =>
                            agentApi.cancelPayout(
                              api,
                              scope.organizationId,
                              scope.agentId,
                              row.id,
                            ),
                          "Payout request cancelled.",
                        );
                        if (cancelled) {
                          setMessage("Payout request cancelled.");
                          payouts.reload();
                          wallet.reload();
                        }
                      }}
                      isLoading={cancellationFeedback.isSubmitting}
                      disabled={cancellationFeedback.isSubmitting}
                    >
                      Cancel
                    </Button>
                  ) : null}
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
          onClick={() => setPage((value) => value - 1)}
        >
          Previous
        </Button>
        <span>Page {page}</span>
        <Button
          variant="secondary"
          disabled={(payouts.data?.length ?? 0) < 20}
          onClick={() => setPage((value) => value + 1)}
        >
          Next
        </Button>
      </div>
      <Dialog
        isOpen={selectedPayoutId !== null}
        title="Payout details"
        description="Provider processing and failure information for this payout request."
        onClose={() => setSelectedPayoutId(null)}
      >
        {payoutDetails.isLoading ? (
          <Loading />
        ) : payoutDetails.error ? (
          <Failure error={payoutDetails.error} retry={payoutDetails.reload} />
        ) : payoutDetails.data ? (
          <dl className="details-list">
            <div>
              <dt>Amount</dt>
              <dd>
                {money(payoutDetails.data.amount, payoutDetails.data.currency)}
              </dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>
                <PayoutStatusBadge value={payoutDetails.data.status} />
              </dd>
            </div>
            <div>
              <dt>Requested</dt>
              <dd>{date(payoutDetails.data.requestedAt)}</dd>
            </div>
            <div>
              <dt>Approved</dt>
              <dd>{date(payoutDetails.data.approvedAt)}</dd>
            </div>
            <div>
              <dt>Processed</dt>
              <dd>{date(payoutDetails.data.processedAt)}</dd>
            </div>
            <div>
              <dt>Provider reference</dt>
              <dd>{payoutDetails.data.providerReference ?? "Not assigned"}</dd>
            </div>
            {payoutDetails.data.failureMessage && (
              <div>
                <dt>Failure</dt>
                <dd>{payoutDetails.data.failureMessage}</dd>
              </div>
            )}
          </dl>
        ) : null}
      </Dialog>
    </div>
  );
}

function PayoutAccountForm({ onSaved }: { onSaved(): void }) {
  const api = useApiClient();
  const scope = useAgentScope();
  const feedback = useFormSubmission();
  return (
    <Card>
      <h2>Add payout account</h2>
      <form
        className="form-grid"
        onSubmit={async (event) => {
          event.preventDefault();
          const form = event.currentTarget;
          const data = new FormData(form);
          const saved = await feedback.submit(
            () =>
              agentApi.registerPayoutAccount(
                api,
                scope.organizationId,
                scope.agentId,
                {
                  method: String(data.get("method")),
                  accountName: String(data.get("accountName")),
                  accountNumber: String(data.get("accountNumber")),
                  bankCode: String(data.get("bankCode")),
                  rail: String(data.get("rail")),
                },
              ),
            "Payout account registered.",
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
          name="method"
          label="Method"
          required
          error={feedback.fieldError("method")}
        >
          <option value="">Select method</option>
          <option value="bank_account">Bank account</option>
        </SelectField>
        <InputField
          name="accountName"
          label="Account name"
          required
          error={feedback.fieldError("accountName")}
        />
        <InputField
          name="accountNumber"
          label="Account number"
          required
          error={feedback.fieldError("accountNumber")}
        />
        <InputField
          name="bankCode"
          label="Bank code"
          required
          error={feedback.fieldError("bankCode")}
        />
        <SelectField
          name="rail"
          label="Transfer rail"
          required
          error={feedback.fieldError("rail")}
        >
          <option value="">Select transfer rail</option>
          <option value="instapay">InstaPay</option>
          <option value="pesonet">PESONet</option>
        </SelectField>
        <Button
          type="submit"
          isLoading={feedback.isSubmitting}
          disabled={feedback.isSubmitting}
        >
          Register account
        </Button>
      </form>
    </Card>
  );
}

function dateInputDaysAgo(days: number) {
  const value = new Date();
  value.setDate(value.getDate() - days);
  return value.toISOString().slice(0, 10);
}

function startOfDay(value: string) {
  return new Date(`${value}T00:00:00`).toISOString();
}

function endOfDay(value: string) {
  return new Date(`${value}T23:59:59.999`).toISOString();
}
