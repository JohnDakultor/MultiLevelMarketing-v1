export interface StorefrontEnvironment {
  apiBaseUrl: string;
  developmentOrganizationSlug?: string;
}

export function storefrontEnvironment(): StorefrontEnvironment {
  return {
    apiBaseUrl: normalizeOptionalUrl(process.env.NEXT_PUBLIC_API_BASE_URL),
    developmentOrganizationSlug:
      process.env.NEXT_PUBLIC_DEVELOPMENT_ORGANIZATION_SLUG?.trim() ||
      undefined,
  };
}

function normalizeOptionalUrl(value: string | undefined): string {
  if (!value?.trim()) return "";
  const url = new URL(value);
  return url.toString().replace(/\/$/, "");
}
