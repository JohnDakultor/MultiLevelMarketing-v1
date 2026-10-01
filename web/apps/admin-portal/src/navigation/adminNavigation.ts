import {
  Capabilities,
  hasCapability,
  type Capability,
} from "@modular-mlm/auth";
import type {
  CurrentUserDto,
  PublicOrganizationConfigDto,
} from "@modular-mlm/contracts";
import type { ApplicationNavigationItem } from "@modular-mlm/design-system";

interface AdminNavigationItem extends ApplicationNavigationItem {
  capability: Capability;
  feature?: keyof Pick<
    PublicOrganizationConfigDto,
    "agentProgramEnabled" | "walletEnabled" | "payoutEnabled"
  >;
}

const navigation: readonly AdminNavigationItem[] = [
  {
    href: "/platform/organizations/new",
    label: "Create organization",
    group: "Platform",
    icon: "organization",
    capability: Capabilities.platformAdministration,
  },
  {
    href: "/",
    label: "Dashboard",
    group: "Overview",
    icon: "dashboard",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/organization",
    label: "Organization",
    group: "Overview",
    icon: "organization",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/catalog",
    label: "Catalog & inventory",
    group: "Commerce",
    icon: "catalog",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/orders",
    label: "Orders & refunds",
    group: "Commerce",
    icon: "orders",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/customers",
    label: "Customers",
    group: "Commerce",
    icon: "customers",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/agents",
    label: "Agents",
    group: "Network",
    icon: "agents",
    capability: Capabilities.organizationAdministration,
    feature: "agentProgramEnabled",
  },
  {
    href: "/compensation",
    label: "Compensation",
    group: "Network",
    icon: "compensation",
    capability: Capabilities.organizationAdministration,
    feature: "agentProgramEnabled",
  },
  {
    href: "/finance",
    label: "Wallets & commissions",
    group: "Finance",
    icon: "finance",
    capability: Capabilities.organizationAdministration,
    feature: "walletEnabled",
  },
  {
    href: "/payouts",
    label: "Payouts",
    group: "Finance",
    icon: "payouts",
    capability: Capabilities.organizationAdministration,
    feature: "payoutEnabled",
  },
  {
    href: "/administrators",
    label: "Administrators",
    group: "Governance",
    icon: "administrators",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/reports",
    label: "Reports",
    group: "Governance",
    icon: "reports",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/audit",
    label: "Audit trail",
    group: "Governance",
    icon: "activity",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/operations",
    label: "Operations",
    group: "System",
    icon: "operations",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/notifications",
    label: "Notifications",
    group: "System",
    icon: "notifications",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/security",
    label: "Sign-in sessions",
    group: "System",
    icon: "security",
    capability: Capabilities.organizationAdministration,
  },
];

export function adminNavigation(
  user: CurrentUserDto,
  organization?: PublicOrganizationConfigDto | null,
): ApplicationNavigationItem[] {
  return navigation.filter(
    (item) =>
      hasCapability(user, item.capability) &&
      (!item.feature || !organization || organization[item.feature] === true),
  );
}
