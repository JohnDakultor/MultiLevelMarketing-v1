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

interface AgentNavigationItem extends ApplicationNavigationItem {
  capability: Capability;
  feature?: keyof Pick<
    PublicOrganizationConfigDto,
    | "agentProgramEnabled"
    | "binaryNetworkEnabled"
    | "walletEnabled"
    | "payoutEnabled"
  >;
}

const navigation: readonly AgentNavigationItem[] = [
  { href: "/", label: "Dashboard", capability: Capabilities.agentWorkspace },
  {
    href: "/network",
    label: "Network",
    capability: Capabilities.agentWorkspace,
    feature: "binaryNetworkEnabled",
  },
  { href: "/sales", label: "Sales", capability: Capabilities.agentWorkspace },
  {
    href: "/earnings",
    label: "Earnings",
    capability: Capabilities.agentWorkspace,
  },
  {
    href: "/wallet",
    label: "Wallet",
    capability: Capabilities.agentWorkspace,
    feature: "walletEnabled",
  },
  {
    href: "/payouts",
    label: "Payouts",
    capability: Capabilities.agentWorkspace,
    feature: "payoutEnabled",
  },
  {
    href: "/referrals",
    label: "Referral tools",
    capability: Capabilities.agentWorkspace,
    feature: "agentProgramEnabled",
  },
  {
    href: "/profile",
    label: "Profile",
    capability: Capabilities.agentWorkspace,
  },
  {
    href: "/notifications",
    label: "Notifications",
    capability: Capabilities.agentWorkspace,
  },
  {
    href: "/security",
    label: "Sign-in sessions",
    capability: Capabilities.agentWorkspace,
  },
];

export function agentNavigation(
  user: CurrentUserDto,
  organization?: PublicOrganizationConfigDto | null,
): ApplicationNavigationItem[] {
  return navigation.filter(
    (item) =>
      hasCapability(user, item.capability) &&
      (!item.feature || !organization || organization[item.feature] === true),
  );
}
