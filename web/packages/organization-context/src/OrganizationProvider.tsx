"use client";

import type { ApiClient } from "@modular-mlm/api-client";
import type { PublicOrganizationConfigDto } from "@modular-mlm/contracts";
import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { resolveOrganization } from "./resolveOrganization";

export interface OrganizationState {
  organization: PublicOrganizationConfigDto | null;
  isLoading: boolean;
  error: Error | null;
}

const OrganizationContext = createContext<OrganizationState | null>(null);

export function OrganizationProvider({
  api,
  hostName,
  developmentSlug,
  children,
}: {
  api: ApiClient;
  hostName: string;
  developmentSlug?: string;
  children: ReactNode;
}) {
  const [organization, setOrganization] =
    useState<PublicOrganizationConfigDto | null>(null);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    void resolveOrganization(api, {
      hostName,
      developmentSlug,
      signal: controller.signal,
    })
      .then((resolved) => {
        setOrganization(resolved);
        applyBranding(resolved);
      })
      .catch((resolutionError: unknown) => {
        if (!controller.signal.aborted) {
          setError(
            resolutionError instanceof Error
              ? resolutionError
              : new Error("Organization could not be resolved."),
          );
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [api, developmentSlug, hostName]);

  const value = useMemo(
    () => ({ organization, isLoading, error }),
    [error, isLoading, organization],
  );

  return (
    <OrganizationContext.Provider value={value}>
      {children}
    </OrganizationContext.Provider>
  );
}

export function useOrganization(): OrganizationState {
  const value = useContext(OrganizationContext);
  if (!value)
    throw new Error(
      "useOrganization must be used inside OrganizationProvider.",
    );
  return value;
}

function applyBranding(organization: PublicOrganizationConfigDto): void {
  document.documentElement.style.setProperty(
    "--brand",
    organization.primaryColor,
  );
  document.documentElement.style.setProperty(
    "--brand-secondary",
    organization.secondaryColor,
  );
  document.documentElement.style.setProperty(
    "--brand-accent",
    organization.accentColor,
  );
}
