import { AdministratorsPage } from "../../features/administrators/AdministratorsPage";
import { AgentOperationsPage } from "../../features/agents/AgentOperationsPage";
import { CompensationPage } from "../../features/compensation/CompensationPage";
import { AdminOrdersPage } from "../../features/orders/AdminOrdersPage";
import { OrganizationSettingsPage } from "../../features/organization/OrganizationSettingsPage";
import { PayoutAdministrationPage } from "../../features/payouts/PayoutAdministrationPage";
import { RouteLoading } from "@modular-mlm/design-system";
import dynamic from "next/dynamic";
import { notFound } from "next/navigation";

const CatalogInventoryPage = dynamic(
  () =>
    import("../../features/catalog/CatalogInventoryPage").then(
      (module) => module.CatalogInventoryPage,
    ),
  { loading: () => <RouteLoading label="Loading catalog and inventory" /> },
);
const AdminReportsPage = dynamic(
  () =>
    import("../../features/reports/AdminReportsPage").then(
      (module) => module.AdminReportsPage,
    ),
  { loading: () => <RouteLoading label="Loading reports" /> },
);
const AuditTrailPage = dynamic(
  () =>
    import("../../features/operations/OperationsPages").then(
      (module) => module.AuditTrailPage,
    ),
  { loading: () => <RouteLoading label="Loading audit trail" /> },
);
const OperationalHealthPage = dynamic(
  () =>
    import("../../features/operations/OperationsPages").then(
      (module) => module.OperationalHealthPage,
    ),
  { loading: () => <RouteLoading label="Loading operational health" /> },
);

export default async function AdminRoute({
  params,
}: {
  params: Promise<{ path: string[] }>;
}) {
  const path = (await params).path;
  if (path.length !== 1) notFound();
  if (path[0] === "organization") return <OrganizationSettingsPage />;
  if (path[0] === "catalog") return <CatalogInventoryPage />;
  if (path[0] === "orders") return <AdminOrdersPage />;
  if (path[0] === "agents") return <AgentOperationsPage />;
  if (path[0] === "compensation") return <CompensationPage />;
  if (path[0] === "payouts") return <PayoutAdministrationPage />;
  if (path[0] === "administrators") return <AdministratorsPage />;
  if (path[0] === "reports") return <AdminReportsPage />;
  if (path[0] === "audit") return <AuditTrailPage />;
  if (path[0] === "operations") return <OperationalHealthPage />;
  notFound();
}
