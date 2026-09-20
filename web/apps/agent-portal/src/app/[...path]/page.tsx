import { AgentFinancePages } from "../../features/finance/route";
import { AgentProfilePage } from "../../features/profile/AgentProfilePage";
import { ReferralToolsPage } from "../../features/referrals/ReferralToolsPage";
import { SalesPage } from "../../features/sales/SalesPage";
import { RouteLoading } from "@modular-mlm/design-system";
import dynamic from "next/dynamic";
import { notFound } from "next/navigation";

const NetworkPage = dynamic(
  () =>
    import("../../features/network/NetworkPage").then(
      (module) => module.NetworkPage,
    ),
  { loading: () => <RouteLoading label="Loading network" /> },
);

export default async function AgentRoute({
  params,
}: {
  params: Promise<{ path: string[] }>;
}) {
  const path = (await params).path;
  if (path.length !== 1) notFound();
  if (path[0] === "network") return <NetworkPage />;
  if (path[0] === "sales") return <SalesPage />;
  if (["earnings", "wallet", "payouts"].includes(path[0]!))
    return (
      <AgentFinancePages
        route={path[0]! as "earnings" | "wallet" | "payouts"}
      />
    );
  if (path[0] === "referrals") return <ReferralToolsPage />;
  if (path[0] === "profile") return <AgentProfilePage />;
  notFound();
}
