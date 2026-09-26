"use client";

import { ApiClient, ApiClientProvider } from "@modular-mlm/api-client";
import {
  authenticationUnauthorizedEvent,
  AuthenticationProvider,
  AuthenticationService,
} from "@modular-mlm/auth";
import { OrganizationProvider } from "@modular-mlm/organization-context";
import { ClientObservability } from "@modular-mlm/observability/client";
import { ConfirmationProvider } from "@modular-mlm/design-system";
import { createContext, useContext, useState, type ReactNode } from "react";
import { adminPortalEnvironment } from "../lib/environment";

const StorefrontUrlContext = createContext<string | undefined>(undefined);

export function AdminPortalProviders({
  hostName,
  organizationSlug,
  storefrontUrl,
  children,
}: {
  hostName: string;
  organizationSlug?: string;
  storefrontUrl: string;
  children: ReactNode;
}) {
  const environment = adminPortalEnvironment();
  const [api] = useState(
    () =>
      new ApiClient({
        baseUrl: environment.apiBaseUrl,
        authentication: "cookie",
        onUnauthorized: () => {
          window.dispatchEvent(new Event(authenticationUnauthorizedEvent));
        },
      }),
  );
  const [authentication] = useState(() => new AuthenticationService(api));

  return (
    <StorefrontUrlContext.Provider value={storefrontUrl}>
      <ClientObservability
        application="admin-portal"
        endpoint={process.env.NEXT_PUBLIC_TELEMETRY_ENDPOINT}
        release={process.env.NEXT_PUBLIC_RELEASE}
      >
        <ConfirmationProvider>
          <ApiClientProvider client={api}>
            <AuthenticationProvider service={authentication}>
              <OrganizationProvider
                api={api}
                hostName={hostName}
                organizationSlug={
                  organizationSlug ?? environment.developmentOrganizationSlug
                }
              >
                {children}
              </OrganizationProvider>
            </AuthenticationProvider>
          </ApiClientProvider>
        </ConfirmationProvider>
      </ClientObservability>
    </StorefrontUrlContext.Provider>
  );
}

export function useAdminStorefrontUrl(): string {
  const storefrontUrl = useContext(StorefrontUrlContext);
  if (!storefrontUrl) {
    throw new Error("AdminPortalProviders must provide the storefront URL.");
  }
  return storefrontUrl;
}
