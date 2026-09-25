import type { Guid, IsoDateTime } from "./common";

export interface SalesBreakdownDto {
  id: Guid;
  label: string;
  orders: number;
  grossSales: number;
}

export interface StatusAmountDto {
  status: string;
  count: number;
  amount: number;
}

export interface CommissionTypeAmountDto {
  type: number;
  count: number;
  amount: number;
}

export interface AdminReportDto {
  organizationId: Guid;
  from: IsoDateTime;
  to: IsoDateTime;
  currency: string;
  orderCount: number;
  grossSales: number;
  refundedAmount: number;
  netSales: number;
  salesByProduct: SalesBreakdownDto[];
  salesByAgent: SalesBreakdownDto[];
  newAgents: number;
  activeAgents: number;
  businessVolume: number;
  commissionExpense: number;
  commissionLiability: number;
  walletLiability: number;
  payouts: StatusAmountDto[];
}

export interface AgentReportDto {
  organizationId: Guid;
  agentId: Guid;
  from: IsoDateTime;
  to: IsoDateTime;
  currency: string;
  attributedOrders: number;
  grossAttributedSales: number;
  refundedAttributedSales: number;
  netAttributedSales: number;
  directRecruits: number;
  downlineSize: number;
  businessVolume: number;
  commissions: CommissionTypeAmountDto[];
  commissionTotal: number;
  payouts: StatusAmountDto[];
}

export interface DashboardTaskDto {
  code: string;
  label: string;
  count: number;
  severity: string;
}

export interface DashboardAlertDto {
  code: string;
  message: string;
  severity: string;
}

export interface AdminDashboardDto {
  organizationId: Guid;
  observedAt: IsoDateTime;
  currency: string;
  salesToday: number;
  salesLast30Days: number;
  ordersToday: number;
  activeAgents: number;
  commissionLiability: number;
  walletLiability: number;
  tasks: DashboardTaskDto[];
  alerts: DashboardAlertDto[];
}
