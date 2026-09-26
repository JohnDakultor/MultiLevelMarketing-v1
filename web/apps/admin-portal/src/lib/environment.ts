export interface AdminPortalEnvironment {
  apiBaseUrl: string;
  developmentOrganizationSlug?: string;
}

export function adminPortalEnvironment(): AdminPortalEnvironment {
  return {
    apiBaseUrl: normalizeOptionalUrl(process.env.NEXT_PUBLIC_API_BASE_URL),
    developmentOrganizationSlug:
      process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim() ||
      undefined,
  };
}

function normalizeOptionalUrl(value: string | undefined): string {
  if (!value?.trim()) return "";
  return new URL(value).toString().replace(/\/$/, "");
}
