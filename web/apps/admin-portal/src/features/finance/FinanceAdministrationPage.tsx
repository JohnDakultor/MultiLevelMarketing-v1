"use client";
import { useApiQuery } from "@modular-mlm/api-client";
import {
  Button,
  Card,
  Checkbox,
  DataTable,
  InputField,
  PageHeader,
  SelectField,
  StatsCard,
  StatusBadge,
} from "@modular-mlm/design-system";
import { useDeferredValue, useState } from "react";
import { adminApi } from "../api/adminApi";
import {
  Failure,
  Loading,
  date,
  money,
  useAdminScope,
} from "../shared/AdminState";
import {
  commissionStatus as commissionStatusPresentation,
  commissionType as commissionTypeLabel,
  walletEntryType,
  walletStatus as walletStatusPresentation,
} from "../shared/status";

export function FinanceAdministrationPage() {
  const scope = useAdminScope();
  const [walletPage, setWalletPage] = useState(1);
  const [commissionPage, setCommissionPage] = useState(1);
  const [search, setSearch] = useState("");
  const deferredSearch = useDeferredValue(search);
  const [walletStatus, setWalletStatus] = useState("");
  const [negativeOnly, setNegativeOnly] = useState(false);
  const [commissionType, setCommissionType] = useState("");
  const [commissionStatus, setCommissionStatus] = useState("");
  const [includeReversals, setIncludeReversals] = useState(true);
  const [agentId, setAgentId] = useState<string | null>(null);
  const wallets = useApiQuery(
    (api, signal) =>
      adminApi.adminWallets(
        api,
        scope.organizationId,
        walletPage,
        deferredSearch,
        signal,
        { status: walletStatus || undefined, negativeOnly },
      ),
    [
      scope.organizationId,
      walletPage,
      deferredSearch,
      walletStatus,
      negativeOnly,
    ],
    scope.isReady,
  );
  const commissions = useApiQuery(
    (api, signal) =>
      adminApi.commissionLedger(
        api,
        scope.organizationId,
        commissionPage,
        signal,
        {
          commissionType: commissionType || undefined,
          status: commissionStatus || undefined,
          includeReversals,
        },
      ),
    [
      scope.organizationId,
      commissionPage,
      commissionType,
      commissionStatus,
      includeReversals,
    ],
    scope.isReady,
  );
  const entries = useApiQuery(
    (api, signal) =>
      adminApi.adminWalletEntries(
        api,
        scope.organizationId,
        agentId ?? "",
        1,
        signal,
      ),
    [scope.organizationId, agentId],
    scope.isReady && agentId !== null,
  );
  if (wallets.isLoading || commissions.isLoading) return <Loading />;
  if (wallets.error)
    return <Failure error={wallets.error} retry={wallets.reload} />;
  if (commissions.error)
    return <Failure error={commissions.error} retry={commissions.reload} />;
  return (
    <div className="content-stack">
      <PageHeader
        eyebrow="Finance"
        title="Wallet and commission ledgers"
        description="Tenant-scoped immutable finance journals for operational support."
      />
      <div className="toolbar ds-filter-bar">
        <InputField
          id="wallet-search"
          label="Search wallets"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
            setWalletPage(1);
          }}
        />
        <SelectField
          id="wallet-status"
          label="Wallet status"
          value={walletStatus}
          onChange={(event) => {
            setWalletStatus(event.target.value);
            setWalletPage(1);
          }}
        >
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Held">Held</option>
          <option value="Closed">Closed</option>
        </SelectField>
        <Checkbox
          id="negative-wallet-balances"
          label="Negative balances only"
          checked={negativeOnly}
          onChange={(event) => {
            setNegativeOnly(event.target.checked);
            setWalletPage(1);
          }}
        />
      </div>
      <DataTable
        caption="Agent wallets"
        rows={wallets.data?.items ?? []}
        rowKey={(row) => row.walletId}
        columns={[
          {
            key: "agent",
            header: "Agent",
            cell: (row) =>
              `${row.agentCode}${row.displayName ? ` · ${row.displayName}` : ""}`,
          },
          {
            key: "status",
            header: "Status",
            cell: (row) => {
              const status = walletStatusPresentation(row.status);
              return <StatusBadge label={status.label} tone={status.tone} />;
            },
          },
          {
            key: "pending",
            header: "Pending",
            cell: (row) => money(row.pending, row.currency),
          },
          {
            key: "available",
            header: "Available",
            cell: (row) => money(row.available, row.currency),
          },
          {
            key: "net",
            header: "Net",
            cell: (row) => money(row.net, row.currency),
          },
          {
            key: "entries",
            header: "",
            cell: (row) => (
              <Button
                variant="secondary"
                onClick={() => setAgentId(row.agentId)}
              >
                View entries
              </Button>
            ),
          },
        ]}
      />
      <Pager
        page={walletPage}
        previous={wallets.data?.hasPreviousPage ?? false}
        next={wallets.data?.hasNextPage ?? false}
        setPage={setWalletPage}
      />
      {agentId && (
        <Card>
          <div className="action-row">
            <h2>Wallet entries</h2>
            <Button variant="ghost" onClick={() => setAgentId(null)}>
              Close
            </Button>
          </div>
          {entries.isLoading ? (
            <Loading />
          ) : entries.error ? (
            <Failure error={entries.error} retry={entries.reload} />
          ) : (
            <DataTable
              caption="Wallet entries"
              rows={entries.data?.items ?? []}
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
                  cell: (row) => walletEntryType(row.type),
                },
                {
                  key: "source",
                  header: "Source",
                  cell: (row) => row.sourceType,
                },
                {
                  key: "amount",
                  header: "Amount",
                  cell: (row) =>
                    money(row.amount, entries.data?.currency ?? scope.currency),
                },
              ]}
            />
          )}
        </Card>
      )}
      <div className="metric-grid">
        <StatsCard
          label="Net commission"
          value={money(
            commissions.data?.totals.netAmount ?? 0,
            commissions.data?.currency ?? scope.currency,
          )}
        />
        <StatsCard
          label="Pending"
          tone="warning"
          value={money(
            commissions.data?.totals.pendingAmount ?? 0,
            commissions.data?.currency ?? scope.currency,
          )}
        />
        <StatsCard
          label="Available"
          tone="success"
          value={money(
            commissions.data?.totals.availableAmount ?? 0,
            commissions.data?.currency ?? scope.currency,
          )}
        />
      </div>
      <div className="toolbar ds-filter-bar">
        <SelectField
          id="commission-type"
          label="Commission type"
          value={commissionType}
          onChange={(event) => {
            setCommissionType(event.target.value);
            setCommissionPage(1);
          }}
        >
          <option value="">All types</option>
          {[
            "DirectSale",
            "BinaryPairing",
            "Bonus",
            "Adjustment",
            "Reversal",
          ].map((value) => (
            <option key={value}>{value}</option>
          ))}
        </SelectField>
        <SelectField
          id="commission-status"
          label="Commission status"
          value={commissionStatus}
          onChange={(event) => {
            setCommissionStatus(event.target.value);
            setCommissionPage(1);
          }}
        >
          <option value="">All statuses</option>
          {["Pending", "Available", "Paid", "Reversed", "Held"].map((value) => (
            <option key={value}>{value}</option>
          ))}
        </SelectField>
        <Checkbox
          id="include-commission-reversals"
          label="Include reversals"
          checked={includeReversals}
          onChange={(event) => {
            setIncludeReversals(event.target.checked);
            setCommissionPage(1);
          }}
        />
      </div>
      <DataTable
        caption="Commission ledger"
        rows={commissions.data?.items ?? []}
        rowKey={(row) => row.id}
        columns={[
          {
            key: "date",
            header: "Created",
            cell: (row) => date(row.createdAt),
          },
          { key: "agent", header: "Agent", cell: (row) => row.agentCode },
          {
            key: "type",
            header: "Type",
            cell: (row) => commissionTypeLabel(row.type),
          },
          {
            key: "status",
            header: "Status",
            cell: (row) => {
              const status = commissionStatusPresentation(row.status);
              return <StatusBadge label={status.label} tone={status.tone} />;
            },
          },
          {
            key: "source",
            header: "Source",
            cell: (row) => row.sourceOrderId ?? row.pairingRunId ?? "—",
          },
          {
            key: "amount",
            header: "Amount",
            cell: (row) =>
              money(row.amount, commissions.data?.currency ?? scope.currency),
          },
        ]}
      />
      <Pager
        page={commissionPage}
        previous={commissions.data?.hasPreviousPage ?? false}
        next={commissions.data?.hasNextPage ?? false}
        setPage={setCommissionPage}
      />
    </div>
  );
}

function Pager({
  page,
  previous,
  next,
  setPage,
}: {
  page: number;
  previous: boolean;
  next: boolean;
  setPage(value: number | ((current: number) => number)): void;
}) {
  return (
    <div className="pagination-row">
      <Button
        variant="secondary"
        disabled={!previous}
        onClick={() => setPage((value) => value - 1)}
      >
        Previous
      </Button>
      <span>Page {page}</span>
      <Button
        variant="secondary"
        disabled={!next}
        onClick={() => setPage((value) => value + 1)}
      >
        Next
      </Button>
    </div>
  );
}
