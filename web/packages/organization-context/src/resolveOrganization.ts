import { ApiError, type ApiClient } from "@modular-mlm/api-client";
import type { PublicOrganizationConfigDto } from "@modular-mlm/contracts";

export interface OrganizationResolutionInput {
  hostName: string;
  organizationSlug?: string;
  signal?: AbortSignal;
}

export async function resolveOrganization(
  api: ApiClient,
  input: OrganizationResolutionInput,
): Promise<PublicOrganizationConfigDto> {
  if (hasConfiguredSlug(input)) return resolveConfiguredSlug(api, input);

  try {
    return await api.request<PublicOrganizationConfigDto>(
      "/api/organizations/public-config",
      {
        anonymous: true,
        signal: input.signal,
        headers: { "X-Forwarded-Host": normalizeHostName(input.hostName) },
      },
    );
  } catch (error) {
    if (!shouldUseConfiguredFallback(error, input)) throw error;
    return resolveConfiguredSlug(api, input);
  }
}

function resolveConfiguredSlug(
  api: ApiClient,
  input: OrganizationResolutionInput,
): Promise<PublicOrganizationConfigDto> {
  return api.request<PublicOrganizationConfigDto>(
    `/api/organizations/${encodeURIComponent(input.organizationSlug!.trim())}/public-config`,
    { anonymous: true, signal: input.signal },
  );
}

export function hasConfiguredSlug(input: OrganizationResolutionInput): boolean {
  return Boolean(input.organizationSlug?.trim());
}

export function normalizeHostName(hostName: string): string {
  return hostName.trim().toLowerCase().replace(/\.$/, "").split(":")[0] ?? "";
}

export function isDevelopmentHost(hostName: string): boolean {
  const normalized = normalizeHostName(hostName);
  return (
    normalized === "localhost" ||
    normalized.endsWith(".localhost") ||
    normalized === "127.0.0.1" ||
    normalized === "::1"
  );
}

function shouldUseConfiguredFallback(
  error: unknown,
  input: OrganizationResolutionInput,
): boolean {
  return (
    error instanceof ApiError &&
    error.status === 404 &&
    hasConfiguredSlug(input)
  );
}
