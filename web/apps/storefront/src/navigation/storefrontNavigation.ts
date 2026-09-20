import {
  Capabilities,
  hasCapability,
  type Capability,
} from "@modular-mlm/auth";
import type { CurrentUserDto } from "@modular-mlm/contracts";
import type { PublicOrganizationConfigDto } from "@modular-mlm/contracts";
import type { ApplicationNavigationItem } from "@modular-mlm/design-system";

interface StorefrontNavigationItem extends ApplicationNavigationItem {
  capability?: Capability;
  requiresAgentProgram?: boolean;
}

const navigation: readonly StorefrontNavigationItem[] = [
  { href: "/", label: "Home" },
  { href: "/products", label: "Products" },
  { href: "/cart", label: "Cart" },
  {
    href: "/account",
    label: "My account",
    capability: Capabilities.customerAccount,
  },
  {
    href: "/account/agent-application",
    label: "Become an Agent",
    capability: Capabilities.customerAccount,
    requiresAgentProgram: true,
  },
  {
    href: "/notifications",
    label: "Notifications",
    capability: Capabilities.customerAccount,
  },
];

export function storefrontNavigation(
  user: CurrentUserDto | null,
  organization?: PublicOrganizationConfigDto | null,
): ApplicationNavigationItem[] {
  return navigation.filter(
    (item) =>
      (!item.capability || hasCapability(user, item.capability)) &&
      (!item.requiresAgentProgram || organization?.agentProgramEnabled),
  );
}
