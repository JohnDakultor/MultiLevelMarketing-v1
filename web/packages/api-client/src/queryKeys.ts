export type QueryKeyPart = string | number | boolean | null | undefined;

export function queryKey(
  organizationId: string | null | undefined,
  resource: string,
  ...parameters: QueryKeyPart[]
): readonly QueryKeyPart[] {
  return [
    "organization",
    organizationId ?? "unresolved",
    resource,
    ...parameters,
  ] as const;
}

export const queryKeys = {
  currentUser: () => ["identity", "current-user"] as const,
  publicOrganization: (hostName: string) =>
    ["organization", "host", hostName.toLowerCase()] as const,
  catalog: (organizationId: string, page: number) =>
    queryKey(organizationId, "catalog", page),
  order: (organizationId: string, orderId: string) =>
    queryKey(organizationId, "order", orderId),
  agentWallet: (organizationId: string, agentId: string) =>
    queryKey(organizationId, "agent-wallet", agentId),
} as const;
