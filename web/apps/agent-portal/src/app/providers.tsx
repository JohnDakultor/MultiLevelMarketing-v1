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
import { useState, type ReactNode } from "react";
import { agentPortalEnvironment } from "../lib/environment";

export function AgentPortalProviders({
  hostName,
  children,
}: {
  hostName: string;
  children: ReactNode;
}) {
  const environment = agentPortalEnvironment();
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
    <ClientObservability
      application="agent-portal"
      endpoint={process.env.NEXT_PUBLIC_TELEMETRY_ENDPOINT}
      release={process.env.NEXT_PUBLIC_RELEASE}
    >
      <ConfirmationProvider>
        <ApiClientProvider client={api}>
          <AuthenticationProvider service={authentication}>
            <OrganizationProvider
              api={api}
              hostName={hostName}
              developmentSlug={environment.developmentOrganizationSlug}
            >
              {children}
            </OrganizationProvider>
          </AuthenticationProvider>
        </ApiClientProvider>
      </ConfirmationProvider>
    </ClientObservability>
  );
}
