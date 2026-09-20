import { EarningsPage, PayoutsPage, WalletPage } from "./AgentFinancePages";

export function AgentFinancePages({
  route,
}: {
  route: "earnings" | "wallet" | "payouts";
}) {
  if (route === "earnings") return <EarningsPage />;
  if (route === "wallet") return <WalletPage />;
  return <PayoutsPage />;
}
