import { ApiError, type ApiClient } from "@modular-mlm/api-client";
import type { PublicOrganizationConfigDto } from "@modular-mlm/contracts";

export interface OrganizationResolutionInput {
  hostName: string;
  developmentSlug?: string;
  signal?: AbortSignal;
}

export async function resolveOrganization(
  api: ApiClient,
  input: OrganizationResolutionInput,
): Promise<PublicOrganizationConfigDto> {
  if (hasDevelopmentSlug(input)) return resolveDevelopmentSlug(api, input);

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
    if (!shouldUseDevelopmentFallback(error, input)) throw error;
    return resolveDevelopmentSlug(api, input);
  }
}

function resolveDevelopmentSlug(
  api: ApiClient,
  input: OrganizationResolutionInput,
): Promise<PublicOrganizationConfigDto> {
  return api.request<PublicOrganizationConfigDto>(
    `/api/organizations/${encodeURIComponent(input.developmentSlug!.trim())}/public-config`,
    { anonymous: true, signal: input.signal },
  );
}

export function hasDevelopmentSlug(
  input: OrganizationResolutionInput,
): boolean {
  return (
    isDevelopmentHost(input.hostName) && Boolean(input.developmentSlug?.trim())
  );
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

function shouldUseDevelopmentFallback(
  error: unknown,
  input: OrganizationResolutionInput,
): boolean {
  return (
    error instanceof ApiError &&
    error.status === 404 &&
    hasDevelopmentSlug(input)
  );
}
