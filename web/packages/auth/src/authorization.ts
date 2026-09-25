import { Roles, type CurrentUserDto, type Role } from "@modular-mlm/contracts";

export type Portal = "storefront" | "agent" | "admin";

export const Capabilities = {
  storefrontBrowse: "storefront:browse",
  customerAccount: "customer:account",
  agentWorkspace: "agent:workspace",
  organizationAdministration: "organization:administration",
  platformAdministration: "platform:administration",
} as const;

export type Capability = (typeof Capabilities)[keyof typeof Capabilities];

export function hasRole(user: CurrentUserDto | null, role: Role): boolean {
  return user?.roles.some((assignedRole) => assignedRole === role) ?? false;
}

export function canAccessPortal(
  user: CurrentUserDto | null,
  portal: Portal,
): boolean {
  if (portal === "storefront") return true;
  if (!user) return false;

  if (portal === "agent") {
    return hasRole(user, Roles.agent) && user.agentId !== null;
  }

  return (
    (hasRole(user, Roles.administrator) ||
      hasRole(user, Roles.platformAdministrator)) &&
    user.organizationId !== null
  );
}

export function preferredPortal(user: CurrentUserDto): Portal {
  if (canAccessPortal(user, "admin")) return "admin";
  if (canAccessPortal(user, "agent")) return "agent";
  return "storefront";
}

export function capabilitiesFor(
  user: CurrentUserDto | null,
): ReadonlySet<Capability> {
  const capabilities = new Set<Capability>([Capabilities.storefrontBrowse]);
  if (!user) return capabilities;

  capabilities.add(Capabilities.customerAccount);
  if (hasRole(user, Roles.agent) && user.agentId) {
    capabilities.add(Capabilities.agentWorkspace);
  }
  if (hasRole(user, Roles.administrator) && user.organizationId) {
    capabilities.add(Capabilities.organizationAdministration);
  }
  if (hasRole(user, Roles.platformAdministrator)) {
    capabilities.add(Capabilities.platformAdministration);
    if (user.organizationId) {
      capabilities.add(Capabilities.organizationAdministration);
    }
  }
  return capabilities;
}

export function hasCapability(
  user: CurrentUserDto | null,
  capability: Capability,
): boolean {
  return capabilitiesFor(user).has(capability);
}

export function canAccessAgentOrganization(
  user: CurrentUserDto | null,
  organizationId: string,
): boolean {
  return (
    canAccessPortal(user, "agent") && user?.organizationId === organizationId
  );
}

export function canAdministerOrganization(
  user: CurrentUserDto | null,
  organizationId: string,
): boolean {
  if (!user || user.organizationId !== organizationId) return false;
  return (
    hasRole(user, Roles.administrator) ||
    hasRole(user, Roles.platformAdministrator)
  );
}
