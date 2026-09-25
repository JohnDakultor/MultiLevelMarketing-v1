"use client";
import { useApiQuery } from "@modular-mlm/api-client";
import { Button, Card, DataTable, InputField, PageHeader } from "@modular-mlm/design-system";
import { useState } from "react";
import { adminApi } from "../api/adminApi";
import { Failure, Loading, date, money, useAdminScope } from "../shared/AdminState";

export function FinanceAdministrationPage() {
  const scope = useAdminScope();
  const [walletPage, setWalletPage] = useState(1);
  const [commissionPage, setCommissionPage] = useState(1);
  const [search, setSearch] = useState("");
  const [agentId, setAgentId] = useState<string | null>(null);
  const wallets = useApiQuery(
    (api, signal) => adminApi.adminWallets(api, scope.organizationId, walletPage, search, signal),
    [scope.organizationId, walletPage, search],
    scope.isReady,
  );
  const commissions = useApiQuery(
    (api, signal) => adminApi.commissionLedger(api, scope.organizationId, commissionPage, signal),
    [scope.organizationId, commissionPage],
    scope.isReady,
  );
  const entries = useApiQuery(
    (api, signal) => adminApi.adminWalletEntries(api, scope.organizationId, agentId ?? "", 1, signal),
    [scope.organizationId, agentId],
    scope.isReady && agentId !== null,
  );
  if (wallets.isLoading || commissions.isLoading) return <Loading />;
  if (wallets.error) return <Failure error={wallets.error} retry={wallets.reload} />;
  if (commissions.error) return <Failure error={commissions.error} retry={commissions.reload} />;
  return (
    <div className="content-stack">
      <PageHeader title="Wallet and commission ledgers" description="Tenant-scoped immutable finance journals for operational support." />
      <InputField label="Search wallets" value={search} onChange={(event) => { setSearch(event.target.value); setWalletPage(1); }} />
      <DataTable caption="Agent wallets" rows={wallets.data?.items ?? []} rowKey={(row) => row.walletId} columns={[
        { key: "agent", header: "Agent", cell: (row) => `${row.agentCode}${row.displayName ? ` · ${row.displayName}` : ""}` },
        { key: "pending", header: "Pending", cell: (row) => money(row.pending, row.currency) },
        { key: "available", header: "Available", cell: (row) => money(row.available, row.currency) },
        { key: "net", header: "Net", cell: (row) => money(row.net, row.currency) },
        { key: "entries", header: "", cell: (row) => <Button variant="secondary" onClick={() => setAgentId(row.agentId)}>View entries</Button> },
      ]} />
      <Pager page={walletPage} previous={wallets.data?.hasPreviousPage ?? false} next={wallets.data?.hasNextPage ?? false} setPage={setWalletPage} />
      {agentId && <Card>
        <div className="action-row"><h2>Wallet entries</h2><Button variant="ghost" onClick={() => setAgentId(null)}>Close</Button></div>
        {entries.isLoading ? <Loading /> : entries.error ? <Failure error={entries.error} retry={entries.reload} /> : (
          <DataTable caption="Wallet entries" rows={entries.data?.items ?? []} rowKey={(row) => row.id} columns={[
            { key: "date", header: "Created", cell: (row) => date(row.createdAt) },
            { key: "type", header: "Type", cell: (row) => row.type },
            { key: "source", header: "Source", cell: (row) => row.sourceType },
            { key: "amount", header: "Amount", cell: (row) => money(row.amount, entries.data?.currency ?? scope.currency) },
          ]} />
        )}
      </Card>}
      <div className="metric-grid">
        <Card><span>Net commission</span><strong className="metric-value">{money(commissions.data?.totals.netAmount ?? 0, commissions.data?.currency ?? scope.currency)}</strong></Card>
        <Card><span>Pending</span><strong className="metric-value">{money(commissions.data?.totals.pendingAmount ?? 0, commissions.data?.currency ?? scope.currency)}</strong></Card>
        <Card><span>Available</span><strong className="metric-value">{money(commissions.data?.totals.availableAmount ?? 0, commissions.data?.currency ?? scope.currency)}</strong></Card>
      </div>
      <DataTable caption="Commission ledger" rows={commissions.data?.items ?? []} rowKey={(row) => row.id} columns={[
        { key: "date", header: "Created", cell: (row) => date(row.createdAt) },
        { key: "agent", header: "Agent", cell: (row) => row.agentCode },
        { key: "type", header: "Type", cell: (row) => row.type },
        { key: "status", header: "Status", cell: (row) => row.status },
        { key: "source", header: "Source", cell: (row) => row.sourceOrderId ?? row.pairingRunId ?? "—" },
        { key: "amount", header: "Amount", cell: (row) => money(row.amount, commissions.data?.currency ?? scope.currency) },
      ]} />
      <Pager page={commissionPage} previous={commissions.data?.hasPreviousPage ?? false} next={commissions.data?.hasNextPage ?? false} setPage={setCommissionPage} />
    </div>
  );
}

function Pager({ page, previous, next, setPage }: { page: number; previous: boolean; next: boolean; setPage(value: number | ((current: number) => number)): void }) {
  return <div className="pagination-row">
    <Button variant="secondary" disabled={!previous} onClick={() => setPage((value) => value - 1)}>Previous</Button>
    <span>Page {page}</span>
    <Button variant="secondary" disabled={!next} onClick={() => setPage((value) => value + 1)}>Next</Button>
  </div>;
}
