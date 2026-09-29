"use client";

import { useApiQuery } from "@modular-mlm/api-client";
import { WalletEntryType } from "@modular-mlm/contracts";
import {
  Button,
  DataTable,
  EmptyState,
  InputField,
  PageHeader,
  SelectField,
  StatsCard,
  StatusBadge,
} from "@modular-mlm/design-system";
import { useState } from "react";
import { agentApi } from "../api/agentApi";
import { Failure, Loading, date, money } from "../shared/AgentScreenState";
import { walletEntryTypeLabel } from "../shared/status";
import { useAgentScope } from "../shared/useAgentScope";

export function AgentTransactionsPage() {
  const scope = useAgentScope();
  const [page, setPage] = useState(1);
  const [entryType, setEntryType] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const filters = {
    entryType: entryType === "" ? undefined : Number(entryType),
    from: from ? new Date(`${from}T00:00:00`).toISOString() : undefined,
    to: to ? new Date(`${to}T23:59:59.999`).toISOString() : undefined,
  };
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
        filters,
        signal,
      ),
    [scope.organizationId, scope.agentId, page, entryType, from, to],
    scope.isReady,
  );

  if (wallet.isLoading || entries.isLoading) return <Loading />;
  if (wallet.error)
    return <Failure error={wallet.error} retry={wallet.reload} />;
  if (entries.error)
    return <Failure error={entries.error} retry={entries.reload} />;

  const currency = wallet.data?.currency ?? scope.currency;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Financial activity"
        title="Transactions"
        description="Credits, holds, debits, payouts, adjustments, and reversals from the immutable wallet ledger."
      />
      <div className="metric-grid">
        <StatsCard
          label="Available"
          value={money(wallet.data?.available ?? 0, currency)}
          tone="success"
        />
        <StatsCard
          label="Pending"
          value={money(wallet.data?.pending ?? 0, currency)}
        />
        <StatsCard
          label="Held"
          value={money(wallet.data?.held ?? 0, currency)}
          tone="warning"
        />
        <StatsCard
          label="Paid lifetime"
          value={money(wallet.data?.paidLifetime ?? 0, currency)}
        />
      </div>
      <section className="filter-panel" aria-label="Transaction filters">
        <SelectField
          id="transaction-type"
          label="Transaction type"
          value={entryType}
          onChange={(event) => {
            setEntryType(event.target.value);
            setPage(1);
          }}
        >
          <option value="">All types</option>
          {Object.values(WalletEntryType).map((value) => (
            <option key={value} value={value}>
              {walletEntryTypeLabel(value)}
            </option>
          ))}
        </SelectField>
        <InputField
          id="transaction-from"
          label="From"
          type="date"
          value={from}
          onChange={(event) => {
            setFrom(event.target.value);
            setPage(1);
          }}
        />
        <InputField
          id="transaction-to"
          label="To"
          type="date"
          min={from || undefined}
          value={to}
          onChange={(event) => {
            setTo(event.target.value);
            setPage(1);
          }}
        />
        {(entryType || from || to) && (
          <Button
            variant="ghost"
            onClick={() => {
              setEntryType("");
              setFrom("");
              setTo("");
              setPage(1);
            }}
          >
            Clear filters
          </Button>
        )}
      </section>
      {!entries.data?.items.length ? (
        <EmptyState
          title="No transactions found"
          description="There is no wallet activity matching the selected filters."
          action={
            entryType || from || to ? (
              <Button
                variant="secondary"
                onClick={() => {
                  setEntryType("");
                  setFrom("");
                  setTo("");
                }}
              >
                Clear filters
              </Button>
            ) : undefined
          }
        />
      ) : (
        <DataTable
          caption="Agent transaction history"
          rows={entries.data.items}
          rowKey={(row) => row.id}
          columns={[
            { key: "date", header: "Date", cell: (row) => date(row.createdAt) },
            {
              key: "type",
              header: "Type",
              cell: (row) => (
                <StatusBadge
                  label={walletEntryTypeLabel(row.type)}
                  tone={entryTone(row.type)}
                />
              ),
            },
            {
              key: "reference",
              header: "Reference",
              cell: (row) => (
                <span>
                  <strong>{row.sourceType}</strong>
                  <br />
                  <small className="breakable">{row.sourceId}</small>
                </span>
              ),
            },
            {
              key: "available",
              header: "Available",
              cell: (row) =>
                row.availableAt ? date(row.availableAt) : "Immediately",
            },
            {
              key: "amount",
              header: "Amount",
              align: "end",
              cell: (row) => (
                <span
                  className={
                    row.amount < 0 ? "amount-negative" : "amount-positive"
                  }
                >
                  {money(row.amount, currency)}
                </span>
              ),
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
        <span>
          Page {entries.data?.page ?? page} of {entries.data?.totalPages ?? 1}
        </span>
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

function entryTone(type: number): "neutral" | "success" | "warning" | "danger" {
  if (type === WalletEntryType.availableCredit) return "success";
  if (type === WalletEntryType.pendingCredit || type === WalletEntryType.hold)
    return "warning";
  if (type === WalletEntryType.reversal) return "danger";
  return "neutral";
}
