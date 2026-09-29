import { EarningsPage, PayoutsPage, WalletPage } from "./AgentFinancePages";
import { AgentTransactionsPage } from "./AgentTransactionsPage";

export function AgentFinancePages({
  route,
}: {
  route: "earnings" | "wallet" | "transactions" | "payouts";
}) {
  if (route === "earnings") return <EarningsPage />;
  if (route === "wallet") return <WalletPage />;
  if (route === "transactions") return <AgentTransactionsPage />;
  return <PayoutsPage />;
}
