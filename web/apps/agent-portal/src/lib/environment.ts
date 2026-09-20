export interface AgentPortalEnvironment {
  apiBaseUrl: string;
  developmentOrganizationSlug?: string;
  storefrontUrl: string;
}

export function agentPortalEnvironment(): AgentPortalEnvironment {
  return {
    apiBaseUrl: normalizeOptionalUrl(process.env.NEXT_PUBLIC_API_BASE_URL),
    developmentOrganizationSlug:
      process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim() ||
      undefined,
    storefrontUrl:
      normalizeOptionalUrl(process.env.NEXT_PUBLIC_STOREFRONT_URL) ||
      "http://localhost:3000",
  };
}

function normalizeOptionalUrl(value: string | undefined): string {
  if (!value?.trim()) return "";
  return new URL(value).toString().replace(/\/$/, "");
}
