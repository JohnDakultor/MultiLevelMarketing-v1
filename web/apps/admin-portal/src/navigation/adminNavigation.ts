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
    capability: Capabilities.platformAdministration,
  },
  {
    href: "/",
    label: "Dashboard",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/organization",
    label: "Organization",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/catalog",
    label: "Catalog & inventory",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/orders",
    label: "Orders & refunds",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/agents",
    label: "Agents",
    capability: Capabilities.organizationAdministration,
    feature: "agentProgramEnabled",
  },
  {
    href: "/compensation",
    label: "Compensation",
    capability: Capabilities.organizationAdministration,
    feature: "agentProgramEnabled",
  },
  {
    href: "/payouts",
    label: "Payouts",
    capability: Capabilities.organizationAdministration,
    feature: "payoutEnabled",
  },
  {
    href: "/administrators",
    label: "Administrators",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/reports",
    label: "Reports",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/audit",
    label: "Audit trail",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/operations",
    label: "Operations",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/notifications",
    label: "Notifications",
    capability: Capabilities.organizationAdministration,
  },
  {
    href: "/security",
    label: "Sign-in sessions",
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
